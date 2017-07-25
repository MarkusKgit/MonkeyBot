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

        private string apiToken = string.Empty;
        int retries = 0;

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

        //first Key is guildID, second Key is UserID
        private Dictionary<ulong, Dictionary<ulong, int>> userScoresCurrent;
        private Dictionary<ulong, Dictionary<ulong, int>> userScoresAllTime;
        public IDictionary<ulong, Dictionary<ulong, int>> UserScoresAllTime
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
            userScoresCurrent = new Dictionary<ulong, Dictionary<ulong, int>>();
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
            await SendMessage($"Noone has answered the question :( The answer was: **{CleanHtmlString(currentQuestion.CorrectAnswer)}**");
            await GetNextQuestionAsync();
        }

        public async Task StopAsync()
        {            
            client.MessageReceived -= Client_MessageReceived;            
            await SaveScoresAsync();
            string msg = "The quiz has ended." + Environment.NewLine 
                + GetCurrentHighScores() + Environment.NewLine 
                + await GetAllTimeHighScoresAsync(5);
            await SendMessage(msg);
            userScoresCurrent.Clear();
            status = TriviaStatus.Stopped;
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
                    await SendMessage($"Question **{currentIndex + 1}** [*{CleanHtmlString(currentQuestion.Category)}*]: {CleanHtmlString(currentQuestion.Question)}? (*true or false*)");
                }
                else if (currentQuestion.Type == QuestionType.MultipleChoice)
                {
                    var answers = currentQuestion.IncorrectAnswers.Append(currentQuestion.CorrectAnswer);
                    Random rand = new Random();
                    var randomizedAnswers = from item in answers orderby rand.Next() select CleanHtmlString(item);
                    string message = $"Question **{currentIndex + 1}** [*{CleanHtmlString(currentQuestion.Category)}*]: {CleanHtmlString(currentQuestion.Question)}?";
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
                if (CleanHtmlString(currentQuestion.CorrectAnswer).ToLower().Trim() == answer.ToLower().Trim())
                {
                    // Answer is correct.
                    AddPointToUser(user);
                    string msg = $"*{user.Username}* is right! The correct answer was: **{CleanHtmlString(currentQuestion.CorrectAnswer)}**";
                    if (currentIndex < questions.Count - 1)
                        msg += Environment.NewLine + GetCurrentHighScores();
                    await SendMessage(msg);                    
                    await GetNextQuestionAsync();
                }
            }
        }

        private void AddPointToUser(IUser user)
        {
            AddPoint(user.Id, userScoresAllTime);
            AddPoint(user.Id, userScoresCurrent);
        }

        private void AddPoint(ulong userID, Dictionary<ulong, Dictionary<ulong, int>> dict)
        {
            if (dict == null)
                dict = new Dictionary<ulong, Dictionary<ulong, int>>();
            if (dict.ContainsKey(guildID))
            {
                var score = dict[guildID];
                if (score == null)
                    score = new Dictionary<ulong, int>();
                if (score.ContainsKey(userID))
                    score[userID]++;
                else
                    score.Add(userID, 1);
                dict[guildID] = score;
            }
            else
            {
                dict.Add(guildID, new Dictionary<ulong, int>());
                dict[guildID].Add(userID, 1);
            }
        }

        private string GetCurrentHighScores()
        {
            if (status == TriviaStatus.Stopped || !userScoresCurrent.ContainsKey(guildID))
                return string.Empty;

            var sortedScores = userScoresCurrent[guildID].OrderByDescending(x => x.Value);
            //sortedScores.Select(x => $"{client.GetUser(x.Key).Username}: {x.Value} points");
            List<string> scoresList = new List<string>();
            foreach (var score in sortedScores)
            {
                if (score.Value == 1)
                    scoresList.Add($"{client.GetUser(score.Key).Username}: 1 point");
                else
                    scoresList.Add($"{client.GetUser(score.Key).Username}: {score.Value} points");
            }
            string scores = $"**Scores**: {string.Join(", ", scoresList.ToList())}";
            return scores;
        }

        public async Task<string> GetAllTimeHighScoresAsync(int count)
        {
            if (userScoresAllTime == null)
                await LoadScoreAsync();
            int correctedCount = Math.Min(count, userScoresAllTime.Count);
            if (correctedCount < 1)
                return "No scores found!";
            var sortedScores = userScoresAllTime[guildID].OrderByDescending(x => x.Value);
            sortedScores.Take(correctedCount);
            List<string> scoresList = new List<string>();
            foreach (var score in sortedScores)
            {
                if (score.Value == 1)
                    scoresList.Add($"{client.GetUser(score.Key).Username}: 1 point");
                else
                    scoresList.Add($"{client.GetUser(score.Key).Username}: {score.Value} points");
            }
            string scores = $"**Top {correctedCount} of all time**:{Environment.NewLine}{string.Join(", ", scoresList)}";            
            return scores;
        }

        private async Task SendMessage(string text)
        {
            await (client.GetGuild(guildID)?.GetChannel(channelID) as SocketTextChannel)?.SendMessageAsync(text);
        }

        private async Task LoadQuestionsAsync(int count)
        {
            if (string.IsNullOrEmpty(apiToken))
                await GetTokenAsync();
            if (count > 50)
                count = 50;
            using (var httpClient = new HttpClient())
            {
                var json = await httpClient.GetStringAsync($"https://opentdb.com/api.php?amount={count}&token={apiToken}");

                if (!string.IsNullOrEmpty(json))
                {
                    var jobject = JObject.Parse(json);
                    var response = await Task.Run(() => JsonConvert.DeserializeObject<OTDBResponse>(json));
                    if (response.Response == TriviaApiResponse.Success)
                        questions.AddRange(response.Questions);
                    else if (retries <= 2 && ( response.Response == TriviaApiResponse.TokenEmpty || response.Response == TriviaApiResponse.TokenNotFound))
                    {
                        await LoadQuestionsAsync(count);
                    }
                    retries++;
                }
            }
        }

        private async Task GetTokenAsync()
        {
            
            using (var httpClient = new HttpClient())
            {
                var json = await httpClient.GetStringAsync("https://opentdb.com/api_token.php?command=request");
                if (!string.IsNullOrEmpty(json))
                {
                    var jobject = JObject.Parse(json);
                    apiToken = (string)jobject.GetValue("token");
                }
            }
        }

        public async Task LoadScoreAsync()
        {
            userScoresAllTime = new Dictionary<ulong, Dictionary<ulong, int>>();
            string filePath = Path.Combine(AppContext.BaseDirectory, persistanceFilename);
            if (!File.Exists(filePath))
                return;     
            try
            {
                var jsonSettings = new JsonSerializerSettings();
                jsonSettings.TypeNameHandling = TypeNameHandling.All;
                string json = await Helpers.ReadTextAsync(filePath);
                userScoresAllTime = JsonConvert.DeserializeObject<Dictionary<ulong, Dictionary<ulong, int>>> (json, jsonSettings);
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

        private string CleanHtmlString(string html)
        {
            return System.Net.WebUtility.HtmlDecode(html);
            //return html
            //    .Replace("&amp;", "&")
            //    .Replace("&lt;", "<")
            //    .Replace("&gt;", ">")
            //    .Replace("&quot;", "\"")
            //    .Replace("&apos;", "'")
            //    .Replace("&#039;", "'");
        }
    }
}