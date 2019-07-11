using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using dokas.FluentStrings;
using MonkeyBot.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    /// <summary>
    /// Manages a single instance of a trivia game in a Discord channel. Uses Open trivia database https://opentdb.com
    /// </summary>
    public class OTDBTriviaInstance : IDisposable
    {
        // The api token enables us to use a session with opentdb so that we don't get the same question twice during a session
        private string apiToken = string.Empty;

        // keeps track of the current retry count for loading questions
        private int loadingRetries;

        private readonly DiscordSocketClient discordClient;
        private readonly DbService dbService;
        private readonly InteractiveService interactiveService;
        private readonly HttpClientHandler httpClientHandler;
        private readonly HttpClient httpClient;

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
        private readonly SocketCommandContext context;

        private readonly TimeSpan timeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Create a new instance of a trivia game in the specified guild's channel. Requires an established connection
        /// </summary>
        /// <param name="context">Message context of the channel where the trivia should be hosted</param>
        /// <param name="db">DB Service instance</param>
        public OTDBTriviaInstance(SocketCommandContext context, DbService db)
        {
            this.context = context;
            discordClient = context.Client;
            dbService = db;
            interactiveService = new InteractiveService(discordClient);
            questions = new List<OTDBQuestion>();
            guildID = context.Guild.Id;
            channelID = context.Channel.Id;
            httpClientHandler = new HttpClientHandler
            {
                Proxy = null,
                UseProxy = false
            };
            httpClient = new HttpClient(httpClientHandler);
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
                await MonkeyHelpers.SendChannelMessageAsync(discordClient, guildID, channelID, "At least one question has to be played");
                return false;
            }
            if (status == TriviaStatus.Running)
            {
                await MonkeyHelpers.SendChannelMessageAsync(discordClient, guildID, channelID, "There is already a quiz running");
                return false;
            }
            this.questionsToPlay = questionsToPlay;
            questions?.Clear();
            var embed = new EmbedBuilder()
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
            await MonkeyHelpers.SendChannelMessageAsync(discordClient, guildID, channelID, "", embed: embed);

            await LoadQuestionsAsync(questionsToPlay);
            if (questions == null || questions.Count == 0)
            {
                await MonkeyHelpers.SendChannelMessageAsync(discordClient, guildID, channelID, "Questions could not be loaded");
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
                return false;
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
                return false;

            var embedBuilder = new EmbedBuilder()
                    .WithColor(new Color(46, 191, 84))
                    .WithTitle("The quiz has ended");

            var currentScores = GetCurrentHighScores();
            if (!currentScores.IsEmpty().OrWhiteSpace())
                embedBuilder.AddField("Final scores:", currentScores, true);

            var globalScores = await GetGlobalHighScoresAsync(int.MaxValue, guildID);
            if (!globalScores.IsEmpty().OrWhiteSpace())
                embedBuilder.AddField("Global top scores:", globalScores);

            await MonkeyHelpers.SendChannelMessageAsync(discordClient, guildID, channelID, "", embed: embedBuilder.Build());

            userScoresCurrent.Clear();
            status = TriviaStatus.Stopped;
            return true;
        }

        private async Task GetNextQuestionAsync()
        {
            if (status == TriviaStatus.Stopped)
                return;
            if (currentQuestionMessage != null)
            {
                interactiveService.RemoveReactionCallback(currentQuestionMessage);
                int points = QuestionToPoints(currentQuestion);

                var embedBuilder = new EmbedBuilder()
                    .WithColor(new Color(46, 191, 84))
                    .WithTitle("Time is up")
                    .WithDescription($"The correct answer was: **{ currentQuestion.CorrectAnswer}**");

                string msg = "";
                if (correctAnswerUsers.Count > 0)
                {
                    correctAnswerUsers.ForEach(async usr => await AddPointsToUserAsync(usr, points));
                    msg = $"*{string.Join(", ", correctAnswerUsers.Select(u => u.Username))}* had it right! Here, have {points} point{(points == 1 ? "" : "s")}.";
                }
                else
                {
                    msg = "No one had it right";
                }
                embedBuilder.AddField("Correct answers", msg, true);
                if (wrongAnswerUsers.Count > 0)
                {
                    wrongAnswerUsers.ForEach(async usr => await AddPointsToUserAsync(usr, -1));
                    msg = $"*{string.Join(", ", wrongAnswerUsers.Select(u => u.Username))}* had it wrong! You lose 1 point.";
                }
                else
                {
                    msg = "No one had it wrong.";
                }
                embedBuilder.AddField("Incorrect answers", msg, true);

                var highScores = GetCurrentHighScores(3);
                if (!highScores.IsEmpty().OrWhiteSpace())
                    embedBuilder.AddField("Top 3:", highScores, true);

                await MonkeyHelpers.SendChannelMessageAsync(discordClient, guildID, channelID, "", embed: embedBuilder.Build());

                correctAnswerUsers.Clear();
                wrongAnswerUsers.Clear();
            }
            if (currentIndex < questionsToPlay)
            {
                if (currentIndex >= questions.Count) // we want to play more questions than available
                    await LoadQuestionsAsync(10); // load more questions
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
                    builder.AddField($"{currentQuestion.Question}", "True or false?");
                    var trueEmoji = new Emoji("👍");
                    var falseEmoji = new Emoji("👎");
                    var correctAnswerEmoji = currentQuestion.CorrectAnswer.ToLowerInvariant() == "true" ? trueEmoji : falseEmoji;

                    currentQuestionMessage = await interactiveService.SendMessageWithReactionCallbacksAsync(context,
                        new ReactionCallbackData("", builder.Build(), false, true, true, timeout, _ => GetNextQuestionAsync())
                            .WithCallback(trueEmoji, (c, r) => CheckAnswer(r, correctAnswerEmoji))
                            .WithCallback(falseEmoji, (c, r) => CheckAnswer(r, correctAnswerEmoji)),
                        false
                    );
                }
                else if (currentQuestion.Type == TriviaQuestionType.MultipleChoice)
                {
                    // add the correct answer to the list of correct answers to form the list of possible answers
                    var answers = currentQuestion.IncorrectAnswers.Append(currentQuestion.CorrectAnswer);
                    Random rand = new Random();
                    // randomize the order of the answers
                    var randomizedAnswers = answers.OrderBy(_ => rand.Next()).ToList();
                    var correctAnswerEmoji = new Emoji(MonkeyHelpers.GetUnicodeRegionalLetter(randomizedAnswers.IndexOf(currentQuestion.CorrectAnswer)));
                    builder.AddField($"{currentQuestion.Question}", string.Join(Environment.NewLine, randomizedAnswers.Select((s, i) => $"{MonkeyHelpers.GetUnicodeRegionalLetter(i)} {s}")));

                    currentQuestionMessage = await interactiveService.SendMessageWithReactionCallbacksAsync(context,
                        new ReactionCallbackData("", builder.Build(), false, true, true, timeout, _ => GetNextQuestionAsync())
                            .WithCallback(new Emoji(MonkeyHelpers.GetUnicodeRegionalLetter(0)), (c, r) => CheckAnswer(r, correctAnswerEmoji))
                            .WithCallback(new Emoji(MonkeyHelpers.GetUnicodeRegionalLetter(1)), (c, r) => CheckAnswer(r, correctAnswerEmoji))
                            .WithCallback(new Emoji(MonkeyHelpers.GetUnicodeRegionalLetter(2)), (c, r) => CheckAnswer(r, correctAnswerEmoji))
                            .WithCallback(new Emoji(MonkeyHelpers.GetUnicodeRegionalLetter(3)), (c, r) => CheckAnswer(r, correctAnswerEmoji))
                            , false);
                }
                currentIndex++;
            }
            else
            {
                await StopTriviaAsync();
            }
        }

        private Task CheckAnswer(SocketReaction r, Emoji correctAnswer)
        {
            if (status == TriviaStatus.Running && currentQuestion != null && r.User.IsSpecified)
            {
                if (r.Emote.Name == correctAnswer.Name)
                    correctAnswerUsers.Add(r.User.Value);
                else
                    wrongAnswerUsers.Add(r.User.Value);
            }
            return Task.CompletedTask;
        }

        private static int QuestionToPoints(ITriviaQuestion question)
        {
            if (question.Type == TriviaQuestionType.TrueFalse)
                return 1;
            else if (question.Type == TriviaQuestionType.MultipleChoice)
            {
                switch (question.Difficulty)
                {
                    case TriviaQuestionDifficulty.Easy:
                        return 1;

                    case TriviaQuestionDifficulty.Medium:
                        return 2;

                    case TriviaQuestionDifficulty.Hard:
                        return 3;

                    default:
                        return 0;
                }
            }
            return 0;
        }

        private async Task AddPointsToUserAsync(IUser user, int pointsToAdd)
        {
            // Add points to current scores and global scores
            AddPointsCurrent(user, userScoresCurrent, pointsToAdd);
            using (var uow = dbService.UnitOfWork)
            {
                var currentScore = await uow.TriviaScores.GetGuildUserScoreAsync(guildID, user.Id);
                //pointsToAdd can be negative -> prevent less than zero points
                if (currentScore == null && pointsToAdd < 0)
                {
                    pointsToAdd = 0;
                }
                else if (currentScore != null && currentScore.Score + pointsToAdd < 0)
                {
                    pointsToAdd = -1 * currentScore.Score;
                }
                await uow.TriviaScores.IncreaseScoreAsync(guildID, user.Id, pointsToAdd);
                await uow.CompleteAsync();
            }
        }

        private static void AddPointsCurrent(IUser user, Dictionary<ulong, int> pointsDict, int pointsToAdd)
        {
            if (pointsDict == null)
                pointsDict = new Dictionary<ulong, int>();
            if (!pointsDict.ContainsKey(user.Id))
                pointsDict.Add(user.Id, pointsToAdd);
            else
                pointsDict[user.Id] += pointsToAdd;
            //pointsToAdd can be negative -> prevent less than zero points
            if (pointsDict[user.Id] < 0)
                pointsDict[user.Id] = 0;
        }

        private string GetCurrentHighScores(int amount = int.MaxValue)
        {
            if (status == TriviaStatus.Stopped || userScoresCurrent.Count < 1)
                return null;
            amount = Math.Min(amount, userScoresCurrent.Count);
            var sortedScores = userScoresCurrent
                .OrderByDescending(x => x.Value)
                .Take(amount)
                .Select((score, pos) => $"**#{pos + 1}: {discordClient.GetUser(score.Key).Username}**: {score.Value} point{(score.Value == 1 ? "" : "s")}");

            return string.Join(", ", sortedScores);
        }

        /// <summary>
        /// Get the current global high scores for the guild
        /// </summary>
        /// <param name="amount">Amount of scores to get (from first place)</param>
        /// <param name="guildId">Id of the guild to get the scores for</param>
        /// <returns></returns>
        public async Task<string> GetGlobalHighScoresAsync(int amount, ulong guildId)
        {
            List<TriviaScore> userScoresAllTime;
            using (var uow = dbService.UnitOfWork)
            {
                userScoresAllTime = await uow.TriviaScores.GetAllForGuildAsync(guildId);
            }
            int correctedCount = Math.Min(amount, userScoresAllTime.Count());
            if (userScoresAllTime == null || correctedCount < 1)
                return null;

            var guild = discordClient.GetGuild(guildId);
            var sortedScores = userScoresAllTime
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
                await GetTokenAsync();
            }
            // Amount of questions per request is limited to 50 by the API
            if (count > 50)
                count = 50;

            var json = await httpClient.GetStringAsync($"https://opentdb.com/api.php?amount={count}&token={apiToken}");

            if (!json.IsEmpty())
            {
                var otdbResponse = await Task.Run(() => JsonConvert.DeserializeObject<OTDBResponse>(json));
                if (otdbResponse.Response == TriviaApiResponse.Success)
                    questions.AddRange(otdbResponse.Questions.Select(CleanQuestion));
                else if ((otdbResponse.Response == TriviaApiResponse.TokenEmpty || otdbResponse.Response == TriviaApiResponse.TokenNotFound) && loadingRetries <= 2)
                {
                    await GetTokenAsync();
                    await LoadQuestionsAsync(count);
                }
                loadingRetries++;
            }
        }

        // Requests a token from the api. With the token a session is managed. During a session it is ensured that no question is received twice
        private async Task GetTokenAsync()
        {
            var json = await httpClient.GetStringAsync("https://opentdb.com/api_token.php?command=request");
            if (!json.IsEmpty())
            {
                var jobject = JObject.Parse(json);
                apiToken = (string)jobject.GetValue("token");
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
            httpClientHandler.Dispose();
            httpClient.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}