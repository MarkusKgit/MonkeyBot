using Discord;
using Discord.WebSocket;
using MonkeyBot.Common;
using MonkeyBot.Trivia;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    /// <summary>
    /// Service that handles Trivias on a per guild and channel basis
    /// Uses Open Trivia database https://opentdb.com
    /// </summary>
    public class OTDBTriviaService : ITriviaService
    {
        private DiscordSocketClient client;

        // holds all trivia instances on a per guild and channel basis
        private Dictionary<CombinedID, OTDBTrivia> trivias;

        public OTDBTriviaService(DiscordSocketClient client)
        {
            this.client = client;
            trivias = new Dictionary<CombinedID, OTDBTrivia>();
        }

        /// <summary>
        /// Start a new trivia with the specified amount of questions in the specified Discord Channel
        /// Returns boolean success
        /// </summary>
        /// <param name="questionsToPlay">Amount of questions to play</param>
        /// <param name="guildID">Id of the Discord Guild</param>
        /// <param name="channelID">Id of the Discord channel where the trivia is played</param>
        /// <returns>success</returns>
        public async Task<bool> StartAsync(int questionsToPlay, ulong guildID, ulong channelID)
        {
            // Create a combination of guildID and channelID to form a unique identifier for each trivia instance
            CombinedID id = new CombinedID(guildID, channelID, null);
            if (!trivias.ContainsKey(id))
                trivias.Add(id, new OTDBTrivia(client, guildID, channelID));
            return await trivias[id].StartAsync(questionsToPlay);
        }

        /// <summary>
        /// Skips the trivia in the specified guild's channel if a trivia is running, otherwise returns false
        /// </summary>
        /// <param name="guildID">Id of the Discord Guild</param>
        /// <param name="channelID">Id of the Discord channel where the trivia is played</param>
        /// <returns>success</returns>
        public async Task<bool> SkipQuestionAsync(ulong guildID, ulong channelID)
        {
            // Create a combination of guildID and channelID to form a unique identifier to retrieve the trivia instance
            CombinedID id = new CombinedID(guildID, channelID, null);
            if (!trivias.ContainsKey(id))
                return false;
            else
                return await trivias[id].SkipQuestionAsync();
        }

        /// <summary>
        /// Stops the trivia in the specified guild's channel if a trivia is running, otherwise returns false
        /// </summary>
        /// <param name="guildID">Id of the Discord Guild</param>
        /// <param name="channelID">Id of the Discord channel where the trivia is played</param>
        /// <returns>success</returns>
        public async Task<bool> StopAsync(ulong guildID, ulong channelID)
        {
            // Create a combination of guildID and channelID to form a unique identifier to retrieve the trivia instance
            CombinedID id = new CombinedID(guildID, channelID, null);
            if (!trivias.ContainsKey(id))
                return false;
            else
            {
                var result = await trivias[id].StopAsync();
                trivias.Remove(id);
                return result;
            }
        }

        /// <summary>
        /// Returns a formated string that contains the specified amount of high scores in the specified guild
        /// </summary>
        /// <param name="count">max number of high scores to get</param>
        /// <param name="guildID">Id of the Discord Guild</param>
        /// <returns></returns>
        public async Task<string> GetAllTimeHighScoresAsync(int count, ulong guildID)
        {
            return (await GetAllTimeHighScoresAsync(client, count, guildID));
        }

        /// <summary>
        /// Returns a formated string that contains the specified amount of high scores in the specified guild
        /// </summary>
        /// <param name="client">DiscordClient instance</param>
        /// <param name="count">max number of high scores to get</param>
        /// <param name="guildID">Id of the Discord Guild</param>
        /// <returns></returns>
        public static async Task<string> GetAllTimeHighScoresAsync(IDiscordClient client, int count, ulong guildID)
        {
            var userScoresAllTime = await LoadScoreAsync(guildID);
            int correctedCount = Math.Min(count, userScoresAllTime.Count);
            if (correctedCount < 1)
                return "No scores found!";
            var sortedScores = userScoresAllTime.OrderByDescending(x => x.Value);
            sortedScores.Take(correctedCount);
            List<string> scoresList = new List<string>();
            foreach (var score in sortedScores)
            {
                var userName = (await client.GetUserAsync(score.Key)).Username;
                if (score.Value == 1)
                    scoresList.Add($"{userName}: 1 point");
                else
                    scoresList.Add($"{userName}: {score.Value} points");
            }
            string scores = $"**Top {correctedCount} of all time**:{Environment.NewLine}{string.Join(", ", scoresList)}";
            return scores;
        }

        /// <summary>
        /// Reads the persisted high scores of the specified guild and returns them as a Dictionary of userID->score
        /// Returns an empty dict if no scores exist
        /// </summary>
        /// <param name="guildID">Id of the Discord Guild</param>
        /// <returns>scores as Dict(userID->score)</returns>
        public static async Task<Dictionary<ulong, int>> LoadScoreAsync(ulong guildID)
        {
            string filePath = GetScoreFilePath(guildID);
            if (!File.Exists(filePath))
                return new Dictionary<ulong, int>();
            try
            {
                var jsonSettings = new JsonSerializerSettings();
                jsonSettings.TypeNameHandling = TypeNameHandling.All;
                string json = await Helpers.ReadTextAsync(filePath);
                return JsonConvert.DeserializeObject<Dictionary<ulong, int>>(json, jsonSettings);
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// Persists the scores for the specified guild
        /// </summary>
        /// <param name="guildID">Id of the Discord Guild</param>
        /// <param name="scores">Dictionary of userID->score containing the scores of the specified guild</param>
        /// <returns></returns>
        public static async Task SaveScoresAsync(ulong guildID, Dictionary<ulong, int> scores)
        {
            string filePath = GetScoreFilePath(guildID);
            try
            {
                var jsonSettings = new JsonSerializerSettings();
                jsonSettings.TypeNameHandling = TypeNameHandling.All;
                var json = JsonConvert.SerializeObject(scores, Formatting.Indented, jsonSettings);
                await Helpers.WriteTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
                throw ex;
            }
        }

        private static string GetScoreFilePath(ulong guildID)
        {
            // Save the scores on a per guild basis -> less chance of load/save race conditions than when writing to a single file
            string fileName = $"TriviaScores-{guildID}.json";
            return Path.Combine(AppContext.BaseDirectory, "TriviaScores", fileName);
        }
    }
}