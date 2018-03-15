using Discord;
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

namespace MonkeyBot.Services.Common.Trivia
{
    /// <summary>
    /// Manages a single instance of a trivia game in a Discord channel. Uses Open trivia database https://opentdb.com
    /// </summary>
    public class OTDBTriviaInstance
    {
        // The api token enables us to use a session with opentdb so that we don't get the same question twice during a session
        private string apiToken = string.Empty;

        // keeps track of the current retry count for loading questions
        private int loadingRetries;

        private readonly DiscordSocketClient discordClient;
        private readonly DbService dbService;

        private readonly List<OTDBQuestion> questions;

        private int questionsToPlay;

        private int currentIndex;
        private OTDBQuestion currentQuestion;

        private TriviaStatus status = TriviaStatus.Stopped;

        // <userID, score>
        private Dictionary<ulong, int> userScoresCurrent;

        private ulong channelID;
        private ulong guildID;

        /// <summary>
        /// Create a new instance of a trivia game in the specified guild's channel. Requires an established connection
        /// </summary>
        /// <param name="client">Running Client instance</param>
        /// <param name="guildID">Id of the Discord guild</param>
        /// <param name="channelID">Id of the Discord channel</param>
        public OTDBTriviaInstance(DiscordSocketClient client, DbService db, ulong guildID, ulong channelID)
        {
            this.discordClient = client;
            this.dbService = db;
            questions = new List<OTDBQuestion>();
            this.guildID = guildID;
            this.channelID = channelID;
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
                await Helpers.SendChannelMessageAsync(discordClient, guildID, channelID, "At least one question has to be played");
                return false;
            }
            this.questionsToPlay = questionsToPlay;
            questions?.Clear();
            await LoadQuestionsAsync(questionsToPlay);
            if (questions == null || questions.Count == 0)
            {
                await Helpers.SendChannelMessageAsync(discordClient, guildID, channelID, "Questions could not be loaded");
                return false;
            }
            userScoresCurrent = new Dictionary<ulong, int>();
            status = TriviaStatus.Running;
            currentIndex = 0;
            discordClient.MessageReceived += Client_MessageReceivedAsync; // Handle the message received for this channel to check for answers
            await Helpers.SendChannelMessageAsync(discordClient, guildID, channelID, $"Starting trivia with {questionsToPlay} questions");
            await GetNextQuestionAsync();
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
            await Helpers.SendChannelMessageAsync(discordClient, guildID, channelID, $"Noone has answered the question :( The answer was: **{CleanHtmlString(currentQuestion.CorrectAnswer)}**");
            await GetNextQuestionAsync();
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
            discordClient.MessageReceived -= Client_MessageReceivedAsync; // Remove the message received handler
            string msg = "The quiz has ended." + Environment.NewLine
                + GetCurrentHighScores() + Environment.NewLine;
            await Helpers.SendChannelMessageAsync(discordClient, guildID, channelID, msg);
            userScoresCurrent.Clear();
            status = TriviaStatus.Stopped;
            return true;
        }

        private async Task GetNextQuestionAsync()
        {
            if (status == TriviaStatus.Stopped)
                return;
            if (currentIndex < questionsToPlay)
            {
                if (currentIndex >= questions.Count) // we want to play more questions than available
                    await LoadQuestionsAsync(10); // load more questions
                currentQuestion = questions.ElementAt(currentIndex);
                var builder = new EmbedBuilder
                {
                    Color = new Color(26, 137, 185),
                    Title = $"Question **{currentIndex + 1}**"
                };
                int points = QuestionToPoints(currentQuestion);
                builder.Description = $"{CleanHtmlString(currentQuestion.Category)} - {currentQuestion.Difficulty} : {points} point{(points == 1 ? "" : "s")}";
                if (currentQuestion.Type == TriviaQuestionType.TrueFalse)
                {
                    builder.AddField($"{CleanHtmlString(currentQuestion.Question)}", "True or false?");
                }
                else if (currentQuestion.Type == TriviaQuestionType.MultipleChoice)
                {
                    // add the correct answer to the list of correct answers to form the list of possible answers
                    var answers = currentQuestion.IncorrectAnswers.Append(currentQuestion.CorrectAnswer);
                    Random rand = new Random();
                    // randomize the order of the answers
                    var randomizedAnswers = from item in answers orderby rand.Next() select CleanHtmlString(item);
                    builder.AddField($"{CleanHtmlString(currentQuestion.Question)}", string.Join(Environment.NewLine, randomizedAnswers));
                }
                await Helpers.SendChannelMessageAsync(discordClient, guildID, channelID, "", false, builder.Build());
                currentIndex++;
                await CheckQuestionTimeOutAsync(currentQuestion);
            }
            else
                await StopTriviaAsync();
        }

        private async Task CheckQuestionTimeOutAsync(OTDBQuestion question)
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            if (question == currentQuestion)
                await SkipQuestionAsync();
        }

        private async Task Client_MessageReceivedAsync(SocketMessage socketMsg)
        {
            var msg = socketMsg as SocketUserMessage;
            if (msg == null) // Check if the received message is from a user.
                return;
            if (msg.Channel?.Id == channelID)
            {
                var result = await CheckAnswerAsync(msg.Content, msg.Author);
                if (result)
                    GetNextQuestionAsync());
            }
        }

        private async Task<bool> CheckAnswerAsync(string answer, IUser user)
        {
            if (status == TriviaStatus.Running && currentQuestion != null && !user.IsBot)
            {
                // answer must be identical to correct answer atm. TODO: Consider allowing partial answers
                if (CleanHtmlString(currentQuestion.CorrectAnswer).ToLower().Trim() == answer.ToLower().Trim())
                {
                    // Answer is correct.
                    await AddPointsToUserAsync(user, QuestionToPoints(currentQuestion));
                    string msg = $"*{user.Username}* is right! The correct answer was: **{CleanHtmlString(currentQuestion.CorrectAnswer)}**";
                    if (currentIndex < questions.Count - 1)
                        msg += Environment.NewLine + GetCurrentHighScores();
                    await Helpers.SendChannelMessageAsync(discordClient, guildID, channelID, msg);
                    return true;
                }
            }
            return false;
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
        }

        private string GetCurrentHighScores()
        {
            if (status == TriviaStatus.Stopped || userScoresCurrent.Count < 1)
                return string.Empty;

            var sortedScores = userScoresCurrent.OrderByDescending(x => x.Value);
            List<string> scoresList = new List<string>();
            foreach (var score in sortedScores)
            {
                scoresList.Add($"{discordClient.GetUser(score.Key).Username}: {score.Value} point{(score.Value == 1 ? "" : "s")}");
            }
            string scores = $"**Scores**: {string.Join(", ", scoresList.ToList())}";
            return scores;
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
            using (var httpClient = new HttpClient())
            {
                var json = await httpClient.GetStringAsync($"https://opentdb.com/api.php?amount={count}&token={apiToken}");

                if (!json.IsEmpty())
                {
                    var otdbResponse = await Task.Run(() => JsonConvert.DeserializeObject<OTDBResponse>(json));
                    if (otdbResponse.Response == TriviaApiResponse.Success)
                        questions.AddRange(otdbResponse.Questions);
                    else if ((otdbResponse.Response == TriviaApiResponse.TokenEmpty || otdbResponse.Response == TriviaApiResponse.TokenNotFound) && (loadingRetries <= 2))
                    {
                        await GetTokenAsync();
                        await LoadQuestionsAsync(count);
                    }
                    loadingRetries++;
                }
            }
        }

        // Requests a token from the api. With the token a session is managed. During a session it is ensured that no question is received twice
        private async Task GetTokenAsync()
        {
            using (var httpClient = new HttpClient())
            {
                var json = await httpClient.GetStringAsync("https://opentdb.com/api_token.php?command=request");
                if (!json.IsEmpty())
                {
                    var jobject = JObject.Parse(json);
                    apiToken = (string)jobject.GetValue("token");
                }
            }
        }

        //Converts all html encoded special characters
        private static string CleanHtmlString(string html)
        {
            return System.Net.WebUtility.HtmlDecode(html);
        }
    }
}