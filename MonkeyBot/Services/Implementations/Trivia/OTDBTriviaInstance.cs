using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using MonkeyBot.Common;
using MonkeyBot.Database;
using MonkeyBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
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

        private readonly SocketCommandContext commandContext;
        private readonly DiscordSocketClient discordClient;
        private readonly MonkeyDBContext dbContext;
        private readonly InteractiveService interactiveService;        
        private readonly IHttpClientFactory clientFactory;

        private readonly List<OTDBQuestion> questions;

        private int questionsToPlay;

        private int currentIndex;
        private OTDBQuestion currentQuestion;
        private IUserMessage currentQuestionMessage;
        private readonly List<IUser> correctAnswerUsers = new List<IUser>();
        private readonly List<IUser> wrongAnswerUsers = new List<IUser>();

        private TriviaStatus status = TriviaStatus.Stopped;

        // <userID, score>
        private Dictionary<ulong, int> userScoresCurrent;
        private readonly ulong channelID;
        private readonly ulong guildID;

        private readonly TimeSpan timeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Create a new instance of a trivia game in the specified guild's channel. Requires an established connection
        /// </summary>
        /// <param name="commandContext">Message context of the channel where the trivia should be hosted</param>
        /// <param name="db">DB Service instance</param>
        public OTDBTriviaInstance(SocketCommandContext commandContext, MonkeyDBContext dbContext, IHttpClientFactory clientFactory)
        {
            this.commandContext = commandContext;
            discordClient = commandContext.Client;
            this.dbContext = dbContext;
            interactiveService = new InteractiveService(discordClient);
            questions = new List<OTDBQuestion>();
            guildID = commandContext.Guild.Id;
            channelID = commandContext.Channel.Id;
            this.clientFactory = clientFactory;            
        }

        /// <summary>
        /// Starts a new quiz with the specified amount of questions
        /// </summary>
        /// <param name="questionsToPlay">Amount of questions to be played (max 50)</param>
        /// <returns>success</returns>
        public async Task<bool> StartTriviaAsync(int questionsToPlay)
        {
            if (questionsToPlay < 1)
            {
                _ = await MonkeyHelpers.SendChannelMessageAsync(discordClient, guildID, channelID, "At least one question has to be played").ConfigureAwait(false);
                return false;
            }
            if (status == TriviaStatus.Running)
            {
                _ = await MonkeyHelpers.SendChannelMessageAsync(discordClient, guildID, channelID, "There is already a quiz running").ConfigureAwait(false);
                return false;
            }
            this.questionsToPlay = questionsToPlay;
            questions?.Clear();
            var embed = new DiscordEmbedBuilder()
                .WithColor(new Color(26, 137, 185))
                .WithTitle("Trivia")
                .WithDescription(
                     $"Starting trivia with {questionsToPlay} question{ (questionsToPlay == 1 ? "" : "s")}." + Environment.NewLine
                    + "- Answer each question by clicking on the corresponding Emoji" + Environment.NewLine
                    + "- Each question has a value of 1-3 points" + Environment.NewLine
                    + $"- You have {timeout.TotalSeconds} seconds for each question."
                    + "- Only your first answer counts!" + Environment.NewLine
                    + "- Each wrong answer will reduce your points by 1 until you are back to zero" + Environment.NewLine
                    )
                .Build();
            _ = await MonkeyHelpers.SendChannelMessageAsync(discordClient, guildID, channelID, "", embed: embed).ConfigureAwait(false);

            await LoadQuestionsAsync(questionsToPlay).ConfigureAwait(false);
            if (questions == null || questions.Count == 0)
            {
                _ = await MonkeyHelpers.SendChannelMessageAsync(discordClient, guildID, channelID, "Questions could not be loaded").ConfigureAwait(false);
                return false;
            }
            userScoresCurrent = new Dictionary<ulong, int>();
            currentQuestionMessage = null;
            correctAnswerUsers.Clear();
            wrongAnswerUsers.Clear();
            currentIndex = 0;
            status = TriviaStatus.Running;
            await GetNextQuestionAsync().ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Skip the current question. Returns false if trivia is not running
        /// </summary>
        /// <returns>success</returns>
        public async Task<bool> SkipQuestionAsync()
        {
            if (!(status == TriviaStatus.Running))
            {
                return false;
            }

            await GetNextQuestionAsync().ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Stops the current trivia. Returns false if trivia is not running
        /// </summary>
        /// <returns>success</returns>
        public async Task<bool> StopTriviaAsync()
        {
            if (!(status == TriviaStatus.Running))
            {
                return false;
            }

            var embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(new Color(46, 191, 84))
                    .WithTitle("The quiz has ended");

            string currentScores = GetCurrentHighScores();
            if (!currentScores.IsEmptyOrWhiteSpace())
            {
                _ = embedBuilder.AddField("Final scores:", currentScores, true);
            }

            string globalScores = await GetGlobalHighScoresAsync(int.MaxValue, guildID).ConfigureAwait(false);
            if (!globalScores.IsEmptyOrWhiteSpace())
            {
                _ = embedBuilder.AddField("Global top scores:", globalScores);
            }

            _ = await MonkeyHelpers.SendChannelMessageAsync(discordClient, guildID, channelID, "", embed: embedBuilder.Build()).ConfigureAwait(false);

            userScoresCurrent.Clear();
            status = TriviaStatus.Stopped;
            return true;
        }

        private async Task GetNextQuestionAsync()
        {
            if (status == TriviaStatus.Stopped)
            {
                return;
            }

            if (currentQuestionMessage != null)
            {
                interactiveService.RemoveReactionCallback(currentQuestionMessage);
                int points = QuestionToPoints(currentQuestion);

                var embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(new Color(46, 191, 84))
                    .WithTitle("Time is up")
                    .WithDescription($"The correct answer was: **{ currentQuestion.CorrectAnswer}**");

                string msg = "";
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

                _ = await MonkeyHelpers.SendChannelMessageAsync(discordClient, guildID, channelID, "", embed: embedBuilder.Build()).ConfigureAwait(false);

                correctAnswerUsers.Clear();
                wrongAnswerUsers.Clear();
            }
            if (currentIndex < questionsToPlay)
            {
                if (currentIndex >= questions.Count) // we want to play more questions than available
                {
                    await LoadQuestionsAsync(10).ConfigureAwait(false); // load more questions
                }

                currentQuestion = questions.ElementAt(currentIndex);
                var builder = new EmbedBuilder
                {
                    Color = new Color(26, 137, 185),
                    Title = $"Question {currentIndex + 1}"
                };
                int points = QuestionToPoints(currentQuestion);
                builder.Description = $"{currentQuestion.Category} - {currentQuestion.Difficulty} : {points} point{(points == 1 ? "" : "s")}";
                if (currentQuestion.Type == TriviaQuestionType.TrueFalse)
                {
                    _ = builder.AddField($"{currentQuestion.Question}", "True or false?");
                    var trueEmoji = new Emoji("👍");
                    var falseEmoji = new Emoji("👎");
                    Emoji correctAnswerEmoji = currentQuestion.CorrectAnswer.Equals("true", StringComparison.OrdinalIgnoreCase) ? trueEmoji : falseEmoji;

                    currentQuestionMessage = await interactiveService.SendMessageWithReactionCallbacksAsync(commandContext,
                        new ReactionCallbackData("", builder.Build(), false, true, true, timeout, _ => GetNextQuestionAsync())
                            .WithCallback(trueEmoji, (c, r) => CheckAnswer(r, correctAnswerEmoji))
                            .WithCallback(falseEmoji, (c, r) => CheckAnswer(r, correctAnswerEmoji)),
                        false
                    ).ConfigureAwait(false);
                }
                else if (currentQuestion.Type == TriviaQuestionType.MultipleChoice)
                {
                    // add the correct answer to the list of correct answers to form the list of possible answers
                    IEnumerable<string> answers = currentQuestion.IncorrectAnswers.Append(currentQuestion.CorrectAnswer);
                    var rand = new Random();
                    // randomize the order of the answers
                    var randomizedAnswers = answers.OrderBy(_ => rand.Next()).ToList();
                    var correctAnswerEmoji = new Emoji(MonkeyHelpers.GetUnicodeRegionalLetter(randomizedAnswers.IndexOf(currentQuestion.CorrectAnswer)));
                    _ = builder.AddField($"{currentQuestion.Question}", string.Join(Environment.NewLine, randomizedAnswers.Select((s, i) => $"{MonkeyHelpers.GetUnicodeRegionalLetter(i)} {s}")));

                    currentQuestionMessage = await interactiveService.SendMessageWithReactionCallbacksAsync(commandContext,
                        new ReactionCallbackData("", builder.Build(), false, true, true, timeout, _ => GetNextQuestionAsync())
                            .WithCallback(new Emoji(MonkeyHelpers.GetUnicodeRegionalLetter(0)), (c, r) => CheckAnswer(r, correctAnswerEmoji))
                            .WithCallback(new Emoji(MonkeyHelpers.GetUnicodeRegionalLetter(1)), (c, r) => CheckAnswer(r, correctAnswerEmoji))
                            .WithCallback(new Emoji(MonkeyHelpers.GetUnicodeRegionalLetter(2)), (c, r) => CheckAnswer(r, correctAnswerEmoji))
                            .WithCallback(new Emoji(MonkeyHelpers.GetUnicodeRegionalLetter(3)), (c, r) => CheckAnswer(r, correctAnswerEmoji))
                            , false).ConfigureAwait(false);
                }
                currentIndex++;
            }
            else
            {
                _ = await StopTriviaAsync().ConfigureAwait(false);
            }
        }

        private Task CheckAnswer(SocketReaction r, Emoji correctAnswer)
        {
            if (status == TriviaStatus.Running && currentQuestion != null && r.User.IsSpecified)
            {
                if (r.Emote.Name == correctAnswer.Name)
                {
                    correctAnswerUsers.Add(r.User.Value);
                }
                else
                {
                    wrongAnswerUsers.Add(r.User.Value);
                }
            }
            return Task.CompletedTask;
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

        private async Task AddPointsToUserAsync(IUser user, int pointsToAdd)
        {
            // Add points to current scores and global scores
            AddPointsCurrent(user, userScoresCurrent, pointsToAdd);

            TriviaScore currentScore = await dbContext.TriviaScores
                .AsQueryable()
                .FirstOrDefaultAsync(s => s.GuildID == guildID && s.UserID == user.Id)
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
                _ = await dbContext.AddAsync(new TriviaScore { GuildID = guildID, UserID = user.Id, Score = pointsToAdd }).ConfigureAwait(false);
            }
            else
            {
                currentScore.Score += pointsToAdd;
                _ = dbContext.Update(currentScore);
            }
            _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);

        }

        private static void AddPointsCurrent(IUser user, Dictionary<ulong, int> pointsDict, int pointsToAdd)
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
                .Select((score, pos) => $"**#{pos + 1}: {discordClient.GetUser(score.Key).Username}**: {score.Value} point{(score.Value == 1 ? "" : "s")}");

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

            SocketGuild guild = discordClient.GetGuild(guildID);
            IEnumerable<string> sortedScores = userScoresAllTime
                .OrderByDescending(x => x.Score)
                .Take(correctedCount)
                .Select((score, pos) => $"**#{pos + 1}: {guild.GetUser(score.UserID)?.Username}** - {score.Score} point{(score.Score == 1 ? "" : "s")}");

            return string.Join(Environment.NewLine, sortedScores);
        }

        // Loads the questions using the otdb API
        private async Task LoadQuestionsAsync(int count)
        {
            if (apiToken.IsEmpty())
            {
                await GetTokenAsync().ConfigureAwait(false);
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
                OTDBResponse otdbResponse = JsonSerializer.Deserialize<OTDBResponse>(json, new JsonSerializerOptions() {
                    Converters = {new OTDBDifficultyConverter(), new OTDBQuestionTypeConverter()}
                });
                
                if (otdbResponse.Response == TriviaApiResponse.Success)
                {
                    questions.AddRange(otdbResponse.Questions.Select(CleanQuestion));
                }
                else if ((otdbResponse.Response == TriviaApiResponse.TokenEmpty || otdbResponse.Response == TriviaApiResponse.TokenNotFound) && loadingRetries <= 2)
                {
                    await GetTokenAsync().ConfigureAwait(false);
                    await LoadQuestionsAsync(count).ConfigureAwait(false);
                }
                loadingRetries++;
            }
        }

        // Requests a token from the api. With the token a session is managed. During a session it is ensured that no question is received twice
        private async Task GetTokenAsync()
        {
            HttpClient httpClient = clientFactory.CreateClient();
            string json = await httpClient.GetStringAsync(tokenUri).ConfigureAwait(false);
            if (!json.IsEmpty())
            {
                var jDocument = JsonDocument.Parse(json);
                apiToken = jDocument.RootElement.GetProperty("token").GetString();
            }
        }

        private static OTDBQuestion CleanQuestion(OTDBQuestion x)
        {
            return new OTDBQuestion
            {
                Category = MonkeyHelpers.CleanHtmlString(x.Category),
                Question = MonkeyHelpers.CleanHtmlString(x.Question),
                CorrectAnswer = MonkeyHelpers.CleanHtmlString(x.CorrectAnswer),
                IncorrectAnswers = x.IncorrectAnswers.Select(MonkeyHelpers.CleanHtmlString).ToList(),
                Type = x.Type,
                Difficulty = x.Difficulty
            };
        }

        public void Dispose()
        {
            interactiveService.Dispose();            
            GC.SuppressFinalize(this);
        }
    }
}