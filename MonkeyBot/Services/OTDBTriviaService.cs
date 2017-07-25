using Discord;
using Discord.WebSocket;
using MonkeyBot.Common;
using MonkeyBot.Trivia;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class OTDBTriviaService : ITriviaService
    {
        private const string persistanceFilename = "TriviaScores.xml";

        private DiscordSocketClient client;
        private ulong guildID;
        private ulong channelID;

        private int questionsToPlay;
        private int currentIndex;
        private OTDBQuestion currentQuestion = null;

        private List<OTDBQuestion> questions;

        public IEnumerable<IQuestion> Questions
        {
            get { return questions; }
        }

        private TriviaStatus status = TriviaStatus.Stopped;
        public TriviaStatus Status { get { return status; } }

        private Dictionary<ulong, int> userScoresCurrent;
        private Dictionary<ulong, int> userScoresAllTime;

        public IDictionary<ulong, int> UserScoresAllTime
        {
            get { return userScoresAllTime; }
        }

        public OTDBTriviaService(DiscordSocketClient client)
        {
            this.client = client;
            questions = new List<OTDBQuestion>();
        }

        public async Task StartAsync(int questionsToPlay, ulong guildID, ulong channelID)
        {
            this.guildID = guildID;
            this.channelID = channelID;
            if (questionsToPlay < 1)
            {
                await SendMessage("At least one question has to be played");
                return;
            }
            this.questionsToPlay = questionsToPlay;
            await LoadQuestionsAsync(questionsToPlay);
            if (questions == null || questions.Count == 0)
            {
                await SendMessage("Questions could not be loaded");
                return;
            }
            userScoresCurrent = new Dictionary<ulong, int>();
            if (userScoresAllTime == null)
                await LoadScoreAsync();
            status = TriviaStatus.Running;
            currentIndex = 0;
            client.MessageReceived += Client_MessageReceived;
            await SendMessage($"Starting trivia with {questionsToPlay} questions");
            await GetNextQuestionAsync();
        }

        public async Task SkipQuestionAsync()
        {
            if (status == TriviaStatus.Stopped)
                return;
            await SendMessage($"Noone has answered the question :( The answer was: {currentQuestion.CorrectAnswer}");
            await GetNextQuestionAsync();
        }

        public async Task StopAsync()
        {
            userScoresCurrent.Clear();
            client.MessageReceived -= Client_MessageReceived;
            status = TriviaStatus.Stopped;
            await SaveScoresAsync();
            string msg = "The quiz has ended." + Environment.NewLine + await GetAllTimeHighScoresAsync(5);
            await SendMessage(msg);
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
                if (currentQuestion.Type == QuestionType.TrueFalse)
                {
                    await SendMessage($"Question {currentIndex + 1} [{currentQuestion.Category}]: {currentQuestion.Question}? (*true or false*)");
                }
                else if (currentQuestion.Type == QuestionType.MultipleChoice)
                {
                    var answers = currentQuestion.IncorrectAnswers.Append(currentQuestion.CorrectAnswer);
                    Random rand = new Random();
                    var randomizedAnswers = from item in answers orderby rand.Next() select item;
                    string message = $"Question {currentIndex + 1} [{currentQuestion.Category}]: {currentQuestion.Question}";
                    message += Environment.NewLine + string.Join(Environment.NewLine, randomizedAnswers);
                    await SendMessage(message);
                }
                currentIndex++;
            }
            else
                await StopAsync();
        }

        private async Task Client_MessageReceived(SocketMessage socketMsg)
        {
            var msg = socketMsg as SocketUserMessage;
            if (msg == null)                                          // Check if the received message is from a user.
                return;
            await CheckAnswer(msg.Content, msg.Author);
        }

        private async Task CheckAnswer(string answer, IUser user)
        {
            if (status == TriviaStatus.Running && currentQuestion != null)
            {
                if (currentQuestion.CorrectAnswer.ToLower() == answer.ToLower())
                {
                    // Answer is correct.
                    string msg = $"{user.Username} is right! The correct answer was: {currentQuestion.CorrectAnswer}";
                    msg += Environment.NewLine + GetCurrentHighScores();
                    await SendMessage(msg);
                    AddPointToUser(user);
                    await GetNextQuestionAsync();
                }
            }
        }

        private void AddPointToUser(IUser user)
        {
            if (userScoresAllTime == null)
                userScoresAllTime = new Dictionary<ulong, int>();
            if (userScoresAllTime.ContainsKey(user.Id))
                userScoresAllTime[user.Id]++;
            else
                userScoresAllTime.Add(user.Id, 1);
        }

        private string GetCurrentHighScores()
        {
            if (status == TriviaStatus.Stopped)
                return string.Empty;
            var sortedScores = userScoresCurrent.OrderByDescending(x => x.Value);
            sortedScores.Select(x => $"{client.GetUser(x.Key)}: {x.Value} points");
            string scores = $"**Current scores**: {string.Join(", ", sortedScores)}";
            return scores;
        }

        public async Task<string> GetAllTimeHighScoresAsync(int count)
        {
            if (userScoresAllTime == null)
                await LoadScoreAsync();
            int correctedCount = Math.Min(count, userScoresAllTime.Count);
            if (correctedCount < 1)
                return "No scores found!";
            var sortedScores = userScoresAllTime.OrderByDescending(x => x.Value);
            sortedScores.Take(correctedCount).Select(x => $"{client.GetUser(x.Key)}: {x.Value} points");
            string scores = $"**Top {correctedCount} of all time**:{Environment.NewLine} {string.Join(", ", sortedScores)}";
            return scores;
        }

        private async Task SendMessage(string text)
        {
            await (client.GetGuild(guildID)?.GetChannel(channelID) as SocketTextChannel)?.SendMessageAsync(text);
        }

        private async Task LoadQuestionsAsync(int count)
        {
            if (count > 50)
                count = 50;
            using (var httpClient = new HttpClient())
            {
                var json = await httpClient.GetStringAsync($"https://opentdb.com/api.php?amount={count}");

                if (!string.IsNullOrEmpty(json))
                {
                    var jobject = JObject.Parse(json);
                    var response = await Task.Run(() => JsonConvert.DeserializeObject<OTDBResponse>(json));
                    if (response.Response == TriviaApiResponse.Success)
                        questions.AddRange(response.Questions);
                }
            }
        }

        public async Task LoadScoreAsync()
        {
            userScoresAllTime = new Dictionary<ulong, int>();
            string filePath = Path.Combine(AppContext.BaseDirectory, persistanceFilename);
            if (!File.Exists(filePath))
                return;
            try
            {
                var jsonSettings = new JsonSerializerSettings();
                jsonSettings.TypeNameHandling = TypeNameHandling.All;
                string json = await Helpers.ReadTextAsync(filePath);
                userScoresAllTime = JsonConvert.DeserializeObject<Dictionary<ulong, int>>(json, jsonSettings);
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
                throw ex;
            }
        }

        public async Task SaveScoresAsync()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, persistanceFilename);
            try
            {
                var jsonSettings = new JsonSerializerSettings();
                jsonSettings.TypeNameHandling = TypeNameHandling.All;
                var json = JsonConvert.SerializeObject(userScoresAllTime, Formatting.Indented, jsonSettings);
                await Helpers.WriteTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
                throw ex;
            }
        }
    }
}