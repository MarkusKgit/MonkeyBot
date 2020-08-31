using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using Microsoft.EntityFrameworkCore;
using MonkeyBot.Common;
using MonkeyBot.Database;
using MonkeyBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace MonkeyBot.Services
{
    /// <summary>
    /// Manages a single instance of a trivia game in a Discord channel. Uses Open trivia database https://opentdb.com
    /// </summary>
    internal sealed class OTDBTriviaInstance : IDisposable
    {
        private readonly Uri tokenUri = new Uri("https://opentdb.com/api_token.php?command=request");
        private readonly Uri baseApiUri = new Uri("https://opentdb.com/api.php");

        // The api token enables us to use a session with opentdb so that we don't get the same question twice during a session
        private string apiToken = string.Empty;

        // keeps track of the current retry count for loading questions
        private int loadingRetries;

        private readonly DiscordClient discordClient;
        private readonly MonkeyDBContext dbContext;
        private readonly InteractivityExtension interactivityExtension;
        private readonly IHttpClientFactory clientFactory;
        private CancellationTokenSource cancellation;

        private int questionsToPlay;
        private List<OTDBQuestion> questions;

        private TriviaStatus status = TriviaStatus.Stopped;

        // <userID, score>
        private Dictionary<ulong, int> userScoresCurrent;
        private readonly ulong channelId;
        private readonly ulong guildId;

        private readonly TimeSpan timeout = TimeSpan.FromSeconds(30);

        private static readonly DiscordEmoji trueEmoji = DiscordEmoji.FromUnicode("👍");
        private static readonly DiscordEmoji falseEmoji = DiscordEmoji.FromUnicode("👎");
        private static readonly DiscordEmoji[] truefalseEmojis = new DiscordEmoji[] { trueEmoji, falseEmoji };

        private static readonly DiscordEmoji[] multipleChoiceEmojis = new[] {
                        DiscordEmoji.FromUnicode(MonkeyHelpers.GetUnicodeRegionalLetter(0)),
                        DiscordEmoji.FromUnicode(MonkeyHelpers.GetUnicodeRegionalLetter(1)),
                        DiscordEmoji.FromUnicode(MonkeyHelpers.GetUnicodeRegionalLetter(2)),
                        DiscordEmoji.FromUnicode(MonkeyHelpers.GetUnicodeRegionalLetter(3)),
                    };

        /// <summary>
        /// Create a new instance of a trivia game in the specified guild's channel. Requires an established connection
        /// </summary>
        /// <param name="commandContext">Message context of the channel where the trivia should be hosted</param>
        /// <param name="db">DB Service instance</param>
        public OTDBTriviaInstance(ulong guildId, ulong channelId, DiscordClient discordClient, MonkeyDBContext dbContext, IHttpClientFactory clientFactory)
        {
            this.guildId = guildId;
            this.channelId = channelId;
            this.discordClient = discordClient;
            this.dbContext = dbContext;
            this.interactivityExtension = discordClient.GetInteractivity();
            this.clientFactory = clientFactory;            
        }

        /// <summary>
        /// Starts a new quiz with the specified amount of questions
        /// </summary>
        /// <param name="questionsToPlay">Amount of questions to be played (max 50)</param>
        /// <returns>success</returns>
        public async Task<bool> StartTriviaAsync(int questionsToPlay)
        {
            cancellation = new CancellationTokenSource();

            if (questionsToPlay < 1)
            {
                _ = await MonkeyHelpers.SendChannelMessageAsync(discordClient, guildId, channelId, "At least one question has to be played").ConfigureAwait(false);
                return false;
            }
            if (status == TriviaStatus.Running)
            {
                _ = await MonkeyHelpers.SendChannelMessageAsync(discordClient, guildId, channelId, "There is already a quiz running").ConfigureAwait(false);
                return false;
            }
            this.questionsToPlay = questionsToPlay;
            questions = new List<OTDBQuestion>(await LoadQuestionsAsync(questionsToPlay).ConfigureAwait(false));
            if (questions == null || questions.Count == 0)
            {
                _ = await MonkeyHelpers.SendChannelMessageAsync(discordClient, guildId, channelId, "Questions could not be loaded").ConfigureAwait(false);
                return false;
            }

            DiscordEmbed embed = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor(26, 137, 185))
                .WithTitle("Trivia")
                .WithDescription(
                     $"Starting trivia with {questionsToPlay} question{ (questionsToPlay == 1 ? "" : "s")}. \n"
                    + "- Answer each question by clicking on the corresponding Emoji \n"
                    + "- Each question has a value of 1-3 points \n"
                    + $"- You have {timeout.TotalSeconds} seconds for each question. \n"                    
                    + "- Each wrong answer will reduce your points by 1 until you are back to zero"
                    )
                .Build();
            _ = await MonkeyHelpers.SendChannelMessageAsync(discordClient, guildId, channelId, embed: embed).ConfigureAwait(false);


            userScoresCurrent = new Dictionary<ulong, int>();
            status = TriviaStatus.Running;
            await PlayTriviaAsync().ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Stops the current trivia.
        /// </summary>
        /// <returns>success</returns>
        public async Task EndTriviaAsync()
        {
            if (!(status == TriviaStatus.Running))
            {
                return;
            }

            if (cancellation != null)
            {
                cancellation.Cancel();
            }

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(new DiscordColor(46, 191, 84))
                    .WithTitle("The quiz has ended");

            string currentScores = GetCurrentHighScores();
            if (!currentScores.IsEmptyOrWhiteSpace())
            {
                _ = embedBuilder.AddField("Final scores:", currentScores, true);
            }

            string globalScores = await GetGlobalHighScoresAsync(int.MaxValue, guildId).ConfigureAwait(false);
            if (!globalScores.IsEmptyOrWhiteSpace())
            {
                _ = embedBuilder.AddField("Global top scores:", globalScores);
            }

            _ = await MonkeyHelpers.SendChannelMessageAsync(discordClient, guildId, channelId, embed: embedBuilder.Build()).ConfigureAwait(false);

            userScoresCurrent.Clear();
            status = TriviaStatus.Stopped;
        }

        private async Task PlayTriviaAsync()
        {
            if (status == TriviaStatus.Stopped)
            {
                return;
            }

            int currentIndex = 0;
            while (currentIndex < questionsToPlay && !cancellation.Token.IsCancellationRequested)
            {
                if (currentIndex >= questions.Count) // we want to play more questions than available
                {
                    questions.AddRange(await LoadQuestionsAsync(10).ConfigureAwait(false)); // load more questions
                }

                OTDBQuestion currentQuestion = questions.ElementAt(currentIndex);
                DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                    .WithColor(new DiscordColor(26, 137, 185))
                    .WithTitle($"Question {currentIndex + 1}");

                int points = QuestionToPoints(currentQuestion);
                builder.Description = $"{currentQuestion.Category} - {currentQuestion.Difficulty} : {points} point{(points == 1 ? "" : "s")}";

                DiscordEmoji[] answerEmojis;
                DiscordEmoji correctAnswerEmoji;
                DiscordMessage m;

                if (currentQuestion.Type == TriviaQuestionType.TrueFalse)
                {
                    _ = builder.AddField($"{currentQuestion.Question}", "True or false?");

                    correctAnswerEmoji = currentQuestion.CorrectAnswer.Equals("true", StringComparison.OrdinalIgnoreCase) ? trueEmoji : falseEmoji;
                    answerEmojis = truefalseEmojis;
                }
                else // TriviaQuestionType.MultipleChoice)
                {
                    // add the correct answer to the list of correct answers to form the list of possible answers
                    IEnumerable<string> answers = currentQuestion.IncorrectAnswers.Append(currentQuestion.CorrectAnswer);
                    var rand = new Random();
                    // randomize the order of the answers
                    var randomizedAnswers = answers.OrderBy(_ => rand.Next())
                                                   .ToList();
                    _ = builder.AddField($"{currentQuestion.Question}", string.Join(Environment.NewLine, randomizedAnswers.Select((s, i) => $"{MonkeyHelpers.GetUnicodeRegionalLetter(i)} {s}")));
                    correctAnswerEmoji = DiscordEmoji.FromUnicode(MonkeyHelpers.GetUnicodeRegionalLetter(randomizedAnswers.IndexOf(currentQuestion.CorrectAnswer)));
                    answerEmojis = multipleChoiceEmojis;
                }

                m = await MonkeyHelpers.SendChannelMessageAsync(discordClient, guildId, channelId, embed: builder.Build()).ConfigureAwait(false);
                var results = await interactivityExtension
                    .DoPollAsync(m, answerEmojis, PollBehaviour.DeleteEmojis, timeout)
                    .WithCancellationAsync(cancellation.Token)
                    .ConfigureAwait(false);
                List<DiscordUser> correctAnswerUsers = results.Where(x => x.Emoji == correctAnswerEmoji)
                                                              .SelectMany(x => x.Voted)
                                                              .ToList();
                List<DiscordUser> wrongAnswerUsers = results.Where(x => x.Emoji != correctAnswerEmoji)
                                                            .SelectMany(x => x.Voted)
                                                            .ToList();

                DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(new DiscordColor(46, 191, 84))
                    .WithTitle("Time is up")
                    .WithDescription($"The correct answer was: **{ currentQuestion.CorrectAnswer}**");

                string msg;
                if (correctAnswerUsers.Count > 0)
                {
                    correctAnswerUsers.ForEach(async usr => await AddPointsToUserAsync(usr, points).ConfigureAwait(false));
                    msg = $"*{string.Join(", ", correctAnswerUsers.Select(u => u.Username))}* had it right! Here, have {points} point{(points == 1 ? "" : "s")}.";
                }
                else
                {
                    msg = "No one had it right";
                }
                _ = embedBuilder.AddField("Correct answers", msg, true);
                if (wrongAnswerUsers.Count > 0)
                {
                    wrongAnswerUsers.ForEach(async usr => await AddPointsToUserAsync(usr, -1).ConfigureAwait(false));
                    msg = $"*{string.Join(", ", wrongAnswerUsers.Select(u => u.Username))}* had it wrong! You lose 1 point.";
                }
                else
                {
                    msg = "No one had it wrong.";
                }
                _ = embedBuilder.AddField("Incorrect answers", msg, true);

                string highScores = GetCurrentHighScores(3);
                if (!highScores.IsEmptyOrWhiteSpace())
                {
                    _ = embedBuilder.AddField("Top 3:", highScores, true);
                }

                _ = await MonkeyHelpers.SendChannelMessageAsync(discordClient, guildId, channelId, embed: embedBuilder.Build()).ConfigureAwait(false);

                currentIndex++;
            }
            
            await EndTriviaAsync().ConfigureAwait(false);
        }

        private static int QuestionToPoints(ITriviaQuestion question)
        {
            return question.Type switch
            {
                TriviaQuestionType.TrueFalse => 1,
                TriviaQuestionType.MultipleChoice =>
                    question.Difficulty switch
                    {
                        TriviaQuestionDifficulty.Easy => 1,
                        TriviaQuestionDifficulty.Medium => 2,
                        TriviaQuestionDifficulty.Hard => 3,
                        _ => 0,
                    },
                _ => 0
            };
        }

        private async Task AddPointsToUserAsync(DiscordUser user, int pointsToAdd)
        {
            // Add points to current scores and global scores
            AddPointsCurrent(user, userScoresCurrent, pointsToAdd);

            TriviaScore currentScore = await dbContext.TriviaScores
                .AsQueryable()
                .FirstOrDefaultAsync(s => s.GuildID == guildId && s.UserID == user.Id)
                .ConfigureAwait(false);
            //pointsToAdd can be negative -> prevent less than zero points
            if (currentScore == null && pointsToAdd < 0)
            {
                pointsToAdd = 0;
            }
            else if (currentScore != null && currentScore.Score + pointsToAdd < 0)
            {
                pointsToAdd = -1 * currentScore.Score;
            }
            if (currentScore == null)
            {
                _ = await dbContext.AddAsync(new TriviaScore { GuildID = guildId, UserID = user.Id, Score = pointsToAdd }).ConfigureAwait(false);
            }
            else
            {
                currentScore.Score += pointsToAdd;
                _ = dbContext.Update(currentScore);
            }
            _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);

        }

        private static void AddPointsCurrent(DiscordUser user, Dictionary<ulong, int> pointsDict, int pointsToAdd)
        {
            pointsDict ??= new Dictionary<ulong, int>();
            if (!pointsDict.ContainsKey(user.Id))
            {
                pointsDict.Add(user.Id, pointsToAdd);
            }
            else
            {
                pointsDict[user.Id] += pointsToAdd;
            }
            //pointsToAdd can be negative -> prevent less than zero points
            if (pointsDict[user.Id] < 0)
            {
                pointsDict[user.Id] = 0;
            }
        }

        private string GetCurrentHighScores(int amount = int.MaxValue)
        {
            if (status == TriviaStatus.Stopped || userScoresCurrent.Count < 1)
            {
                return null;
            }

            amount = Math.Min(amount, userScoresCurrent.Count);
            IEnumerable<string> sortedScores = userScoresCurrent
                .OrderByDescending(x => x.Value)
                .Take(amount)
                .Select((score, pos) => $"**#{pos + 1}: {(discordClient.Guilds[guildId].Members[score.Key]).Username}**: {score.Value} point{(score.Value == 1 ? "" : "s")}");

            return string.Join(", ", sortedScores);
        }

        /// <summary>
        /// Get the current global high scores for the guild
        /// </summary>
        /// <param name="amount">Amount of scores to get (from first place)</param>
        /// <param name="guildID">Id of the guild to get the scores for</param>
        /// <returns></returns>
        public async Task<string> GetGlobalHighScoresAsync(int amount, ulong guildID)
        {
            List<TriviaScore> userScoresAllTime = await dbContext.TriviaScores
                .AsQueryable()
                .Where(s => s.GuildID == guildID)
                .ToListAsync()
                .ConfigureAwait(false);
            if (userScoresAllTime == null)
            {
                return null;
            }

            int correctedCount = Math.Min(amount, userScoresAllTime.Count);
            if (correctedCount < 1)
            {
                return null;
            }

            DiscordGuild guild = await discordClient.GetGuildAsync(guildId).ConfigureAwait(false);
            IEnumerable<string> sortedScores = userScoresAllTime
                .OrderByDescending(x => x.Score)
                .Take(correctedCount)
                .Select((score, pos) => $"**#{pos + 1}: {(guild.Members[score.UserID])?.Username}**: {score.Score} point{(score.Score == 1 ? "" : "s")}");

            return string.Join(Environment.NewLine, sortedScores);
        }

        // Loads the questions using the otdb API
        private async Task<List<OTDBQuestion>> LoadQuestionsAsync(int count)
        {
            if (apiToken.IsEmpty())
            {
                apiToken = await GetTokenAsync().ConfigureAwait(false);
            }
            // Amount of questions per request is limited to 50 by the API
            if (count > 50)
            {
                count = 50;
            }

            var builder = new UriBuilder(baseApiUri);
            System.Collections.Specialized.NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);
            query["amount"] = count.ToString();
            query["token"] = apiToken;
            builder.Query = query.ToString();
            HttpClient httpClient = clientFactory.CreateClient();
            string json = await httpClient.GetStringAsync(builder.Uri).ConfigureAwait(false);

            if (!json.IsEmpty())
            {
                OTDBResponse otdbResponse = JsonSerializer.Deserialize<OTDBResponse>(json, new JsonSerializerOptions()
                {
                    Converters = { new OTDBDifficultyConverter(), new OTDBQuestionTypeConverter() }
                });

                if (otdbResponse.Response == TriviaApiResponse.Success)
                {
                    return otdbResponse.Questions.Select(CleanQuestion).ToList();
                }
                else if ((otdbResponse.Response == TriviaApiResponse.TokenEmpty || otdbResponse.Response == TriviaApiResponse.TokenNotFound) && loadingRetries <= 2)
                {
                    apiToken = await GetTokenAsync().ConfigureAwait(false);
                    return await LoadQuestionsAsync(count).ConfigureAwait(false);
                }
                loadingRetries++;
            }
            return null;
        }

        // Requests a token from the api. With the token a session is managed. During a session it is ensured that no question is received twice
        private async Task<string> GetTokenAsync()
        {
            HttpClient httpClient = clientFactory.CreateClient();
            string json = await httpClient.GetStringAsync(tokenUri).ConfigureAwait(false);
            if (!json.IsEmpty())
            {
                var jDocument = JsonDocument.Parse(json);
                return jDocument.RootElement.GetProperty("token").GetString();
            }
            return string.Empty;
        }

        private static OTDBQuestion CleanQuestion(OTDBQuestion x)
            => new OTDBQuestion
            {
                Category = MonkeyHelpers.CleanHtmlString(x.Category),
                Question = MonkeyHelpers.CleanHtmlString(x.Question),
                CorrectAnswer = MonkeyHelpers.CleanHtmlString(x.CorrectAnswer),
                IncorrectAnswers = x.IncorrectAnswers.Select(MonkeyHelpers.CleanHtmlString).ToList(),
                Type = x.Type,
                Difficulty = x.Difficulty
            };
        public void Dispose()
        {
            cancellation.Dispose();
        }
    }
}