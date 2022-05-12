using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;
using MonkeyBot.Common;
using MonkeyBot.Database;
using MonkeyBot.Models;
using System;
using System.Collections.Concurrent;
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
        private readonly Uri _tokenUri = new("https://opentdb.com/api_token.php?command=request");
        private readonly Uri _baseApiUri = new("https://opentdb.com/api.php");

        // The api token enables us to use a session with opentdb so that we don't get the same question twice during a session
        private string _apiToken = string.Empty;

        // keeps track of the current retry count for loading questions
        private int _loadingRetries;

        private readonly DiscordClient _discordClient;
        private readonly MonkeyDBContext _dbContext;
        private readonly IHttpClientFactory _clientFactory;
        private CancellationTokenSource _cancellation;

        private int _questionsToPlay;
        private List<OTDBQuestion> _questions;

        private TriviaStatus _status = TriviaStatus.Stopped;

        // <userID, score>
        private ConcurrentDictionary<DiscordMember, string> userAnswers = new();
        private Dictionary<ulong, int> _userScoresCurrent;
        private readonly ulong _channelId;
        private readonly ulong _guildId;

        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

        private static readonly DiscordButtonComponent[] trueFalseButtons = new DiscordButtonComponent[] 
        { 
            new(ButtonStyle.Primary, "Trivia_Answer_True", "👍"), 
            new(ButtonStyle.Primary, "Trivia_Answer_False", "👎") 
        };

        private static readonly DiscordButtonComponent[] multipleChoiceButtons = new DiscordButtonComponent[]
        {
            new(ButtonStyle.Primary, "Trivia_Answer_A", "A"),
            new(ButtonStyle.Primary, "Trivia_Answer_B", "B"),
            new(ButtonStyle.Primary, "Trivia_Answer_C", "C"),
            new(ButtonStyle.Primary, "Trivia_Answer_D", "D")
        };

        private static readonly string[] multipleChoiceOptions = new string[] {"A", "B", "C", "D"};

        private DiscordMessage currentQuestionMessage;

        //TODO: Convert this to using buttons

        /// <summary>
        /// Create a new instance of a trivia game in the specified guild's channel. Requires an established connection
        /// </summary>
        /// <param name="commandContext">Message context of the channel where the trivia should be hosted</param>
        /// <param name="db">DB Service instance</param>
        public OTDBTriviaInstance(ulong guildId, ulong channelId, DiscordClient discordClient, MonkeyDBContext dbContext, IHttpClientFactory clientFactory)
        {
            _guildId = guildId;
            _channelId = channelId;
            _discordClient = discordClient;
            _dbContext = dbContext;            
            _clientFactory = clientFactory;            
        }

        /// <summary>
        /// Starts a new quiz with the specified amount of questions
        /// </summary>
        /// <param name="questionsToPlay">Amount of questions to be played (max 50)</param>
        /// <returns>success</returns>
        public async Task<bool> StartTriviaAsync(int questionsToPlay)
        {
            _cancellation = new CancellationTokenSource();

            if (questionsToPlay < 1)
            {
                await MonkeyHelpers.SendChannelMessageAsync(_discordClient, _guildId, _channelId, "At least one question has to be played");
                return false;
            }
            if (_status == TriviaStatus.Running)
            {
                await MonkeyHelpers.SendChannelMessageAsync(_discordClient, _guildId, _channelId, "There is already a quiz running");
                return false;
            }
            _questionsToPlay = questionsToPlay;
            _questions = new List<OTDBQuestion>(await LoadQuestionsAsync(questionsToPlay));
            if (_questions == null || _questions.Count == 0)
            {
                await MonkeyHelpers.SendChannelMessageAsync(_discordClient, _guildId, _channelId, "Questions could not be loaded");
                return false;
            }

            DiscordEmbed embed = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor(26, 137, 185))
                .WithTitle("Trivia")
                .WithDescription(
                     $"Starting trivia with {questionsToPlay} question{ (questionsToPlay == 1 ? "" : "s")}. \n"
                    + $"- Answer each question by clicking on the corresponding Button \n"
                    + $"- Each question has a value of 1-3 points \n"
                    + $"- You have {_timeout.TotalSeconds} seconds for each question. \n"
                    + $"- Only your first answer counts! Choose wisely. \n"
                    + "- Each wrong answer will reduce your points by 1 until you are back to zero"
                    )
                .Build();
            await MonkeyHelpers.SendChannelMessageAsync(_discordClient, _guildId, _channelId, embed: embed);
            await MonkeyHelpers.TriggerTypingAsync(_discordClient, _guildId, _channelId);
            await Task.Delay(TimeSpan.FromSeconds(5));
            _userScoresCurrent = new Dictionary<ulong, int>();
            _status = TriviaStatus.Running;
            _discordClient.ComponentInteractionCreated += ComponentInteractionCreated;
            await PlayTriviaAsync();
            return true;
        }

        /// <summary>
        /// Stops the current trivia.
        /// </summary>
        /// <returns>success</returns>
        public async Task EndTriviaAsync()
        {
            if (!(_status == TriviaStatus.Running))
            {
                return;
            }

            _discordClient.ComponentInteractionCreated -= ComponentInteractionCreated;

            if (_cancellation != null)
            {
                _cancellation.Cancel();
            }

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(new DiscordColor(46, 191, 84))
                    .WithTitle("The quiz has ended");

            string currentScores = GetCurrentHighScores();
            if (!currentScores.IsEmptyOrWhiteSpace())
            {
                embedBuilder.AddField("Final scores:", currentScores, true);
            }

            string globalScores = await GetGlobalHighScoresAsync(int.MaxValue, _guildId);
            if (!globalScores.IsEmptyOrWhiteSpace())
            {
                embedBuilder.AddField("Global top scores:", globalScores);
            }

            await MonkeyHelpers.SendChannelMessageAsync(_discordClient, _guildId, _channelId, embed: embedBuilder.Build());

            _userScoresCurrent.Clear();
            _status = TriviaStatus.Stopped;
        }

        private async Task PlayTriviaAsync()
        {
            if (_status == TriviaStatus.Stopped)
            {
                return;
            }

            int currentIndex = 0;
            while (currentIndex < _questionsToPlay && !_cancellation.Token.IsCancellationRequested)
            {
                if (currentIndex >= _questions.Count) // we want to play more questions than available
                {
                    _questions.AddRange(await LoadQuestionsAsync(10)); // load more questions
                }

                OTDBQuestion currentQuestion = _questions.ElementAt(currentIndex);
                DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(new DiscordColor(26, 137, 185))
                    .WithTitle($"Question {currentIndex + 1}");

                int points = QuestionToPoints(currentQuestion);
                embedBuilder.Description = $"{currentQuestion.Category} - {currentQuestion.Difficulty} : {points} point{(points == 1 ? "" : "s")}";
                DiscordButtonComponent[] answerButtons;

                string correctAnswer = "";
                if (currentQuestion.Type == TriviaQuestionType.TrueFalse)
                {
                    embedBuilder.AddField($"{currentQuestion.Question}", "True or false?");
                    answerButtons = trueFalseButtons;
                    correctAnswer = currentQuestion.CorrectAnswer;
                }
                else // TriviaQuestionType.MultipleChoice)
                {
                    // add the correct answer to the list of correct answers to form the list of possible answers
                    IEnumerable<string> answers = currentQuestion.IncorrectAnswers.Append(currentQuestion.CorrectAnswer);
                    var rand = new Random();
                    // randomize the order of the answers
                    var randomizedAnswers = answers.OrderBy(_ => rand.Next())
                                                   .ToList();
                    embedBuilder.AddField($"{currentQuestion.Question}", string.Join(Environment.NewLine, randomizedAnswers.Select((s, i) => $"{Formatter.Bold(multipleChoiceOptions[i])}: {s}")));
                    //DEBUG:
                    //embedBuilder.AddField("Correct Answer:", currentQuestion.CorrectAnswer);
                    answerButtons = multipleChoiceButtons;
                    correctAnswer = multipleChoiceOptions[randomizedAnswers.IndexOf(currentQuestion.CorrectAnswer)];
                }
                var msgEmbed = embedBuilder.Build();
                var msgBuilder = new DiscordMessageBuilder().WithEmbed(msgEmbed).AddComponents(answerButtons);                
                //TODO: cache
                var guild = await _discordClient.GetGuildAsync(_guildId);
                var channel = guild.GetChannel(_channelId);
                currentQuestionMessage = await msgBuilder.SendAsync(channel);
                userAnswers.Clear();
                // Give the users time to answer and listen to the Interaction events to collect answers
                await Task.Delay(_timeout);
                await currentQuestionMessage.ModifyAsync(b => b.WithEmbed(msgEmbed));
                                
                List<DiscordMember> correctAnswerUsers = userAnswers.Where(x => x.Value == correctAnswer)
                                                              .Select(x => x.Key)
                                                              .ToList();
                List<DiscordMember> wrongAnswerUsers = userAnswers.Where(x => x.Value != correctAnswer)
                                                            .Select(x => x.Key)
                                                            .ToList();

                embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(new DiscordColor(46, 191, 84))
                    .WithTitle("Time is up")
                    .WithDescription($"The correct answer was: **{ currentQuestion.CorrectAnswer}**");

                string msg;
                if (correctAnswerUsers.Count > 0)
                {
                    correctAnswerUsers.ForEach(async usr => await AddPointsToUserAsync(usr, points));
                    msg = $"*{string.Join(", ", correctAnswerUsers.Select(u => u.DisplayName))}* had it right! Here, have {points} point{(points == 1 ? "" : "s")}.";
                }
                else
                {
                    msg = "No one had it right";
                }
                embedBuilder.AddField("Correct answers", msg, true);
                if (wrongAnswerUsers.Count > 0)
                {
                    wrongAnswerUsers.ForEach(async usr => await AddPointsToUserAsync(usr, -1));
                    msg = $"*{string.Join(", ", wrongAnswerUsers.Select(u => u.DisplayName))}* had it wrong! You lose 1 point.";
                }
                else
                {
                    msg = "No one had it wrong.";
                }
                embedBuilder.AddField("Incorrect answers", msg, true);

                string highScores = GetCurrentHighScores(3);
                if (!highScores.IsEmptyOrWhiteSpace())
                {
                    embedBuilder.AddField("Top 3:", highScores, true);
                }

                await MonkeyHelpers.SendChannelMessageAsync(_discordClient, _guildId, _channelId, embed: embedBuilder.Build());

                currentIndex++;
            }
            
            await EndTriviaAsync();
        }

        private async Task ComponentInteractionCreated(DiscordClient sender, DSharpPlus.EventArgs.ComponentInteractionCreateEventArgs e)
        {            
            if (e.Message != currentQuestionMessage || !e.Id.StartsWith("Trivia_Answer_"))
                return;

            var member = await e.Guild.GetMemberAsync(e.User.Id);
            string answer = e.Id.Substring("Trivia_Answer_".Length);
            if (!userAnswers.ContainsKey(member))
            {
                userAnswers.TryAdd(member, answer);
                await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"You answered {answer}").AsEphemeral());
            }
            else
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Only your first answer counts! Choose wisely").AsEphemeral());
            }
            
            
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
            AddPointsCurrent(user, _userScoresCurrent, pointsToAdd);

            int currentGameScore = _userScoresCurrent[user.Id];
            TriviaScore currentDBScore = await _dbContext.TriviaScores
                .AsQueryable()
                .FirstOrDefaultAsync(s => s.GuildID == _guildId && s.UserID == user.Id)
                ;
            //pointsToAdd can be negative -> prevent less than zero points. Also don't deduct points if the current game has zero points already
            if ((currentDBScore == null || currentGameScore == 0) && pointsToAdd < 0)
            {
                pointsToAdd = 0;
            }            
            else if (currentDBScore != null && currentDBScore.Score + pointsToAdd < 0)
            {
                pointsToAdd = -1 * currentDBScore.Score;
            }
            if (currentDBScore == null)
            {
                await _dbContext.AddAsync(new TriviaScore { GuildID = _guildId, UserID = user.Id, Score = pointsToAdd });
            }
            else
            {
                currentDBScore.Score += pointsToAdd;
                _dbContext.Update(currentDBScore);
            }
            await _dbContext.SaveChangesAsync();

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
            if (_status == TriviaStatus.Stopped || _userScoresCurrent.Count < 1)
            {
                return null;
            }

            amount = Math.Min(amount, _userScoresCurrent.Count);
            IEnumerable<string> sortedScores = _userScoresCurrent
                .OrderByDescending(x => x.Value)
                .Take(amount)
                .Select((score, pos) => $"**#{pos + 1}: {(_discordClient.Guilds[_guildId].Members[score.Key]).DisplayName}**: {score.Value} point{(score.Value == 1 ? "" : "s")}");

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
            List<TriviaScore> userScoresAllTime = await _dbContext.TriviaScores
                .AsQueryable()
                .Where(s => s.GuildID == guildID)
                .ToListAsync()
                ;
            if (userScoresAllTime == null)
            {
                return null;
            }

            int correctedCount = Math.Min(amount, userScoresAllTime.Count);
            if (correctedCount < 1)
            {
                return null;
            }

            DiscordGuild guild = await _discordClient.GetGuildAsync(_guildId);
            IEnumerable<string> sortedScores = userScoresAllTime
                .OrderByDescending(x => x.Score)
                .Take(correctedCount)
                .Select((score, pos) => $"**#{pos + 1}: {(guild.Members[score.UserID])?.DisplayName}**: {score.Score} point{(score.Score == 1 ? "" : "s")}");

            return string.Join(Environment.NewLine, sortedScores);
        }

        // Loads the questions using the otdb API
        private async Task<List<OTDBQuestion>> LoadQuestionsAsync(int count)
        {
            if (_apiToken.IsEmpty())
            {
                _apiToken = await GetTokenAsync();
            }
            // Amount of questions per request is limited to 50 by the API
            if (count > 50)
            {
                count = 50;
            }

            var builder = new UriBuilder(_baseApiUri);
            System.Collections.Specialized.NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);
            query["amount"] = count.ToString();
            query["token"] = _apiToken;
            builder.Query = query.ToString();
            HttpClient httpClient = _clientFactory.CreateClient();
            string json = await httpClient.GetStringAsync(builder.Uri);

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
                else if ((otdbResponse.Response == TriviaApiResponse.TokenEmpty || otdbResponse.Response == TriviaApiResponse.TokenNotFound) && _loadingRetries <= 2)
                {
                    _apiToken = await GetTokenAsync();
                    return await LoadQuestionsAsync(count);
                }
                _loadingRetries++;
            }
            return null;
        }

        // Requests a token from the api. With the token a session is managed. During a session it is ensured that no question is received twice
        private async Task<string> GetTokenAsync()
        {
            HttpClient httpClient = _clientFactory.CreateClient();
            string json = await httpClient.GetStringAsync(_tokenUri);
            if (!json.IsEmpty())
            {
                var jDocument = JsonDocument.Parse(json);
                return jDocument.RootElement.GetProperty("token").GetString();
            }
            return string.Empty;
        }

        private static OTDBQuestion CleanQuestion(OTDBQuestion x)
            => new()
            {
                Category = MonkeyHelpers.CleanHtmlString(x.Category),
                Question = MonkeyHelpers.CleanHtmlString(x.Question),
                CorrectAnswer = MonkeyHelpers.CleanHtmlString(x.CorrectAnswer),
                IncorrectAnswers = x.IncorrectAnswers.Select(MonkeyHelpers.CleanHtmlString).ToList(),
                Type = x.Type,
                Difficulty = x.Difficulty
            };
        public void Dispose() => _cancellation.Dispose();
    }
}