using Discord.WebSocket;
using MonkeyBot.Common;
using MonkeyBot.Services.Common.Trivia;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    /// <summary>
    /// Service that handles Trivias on a per guild and channel basis
    /// Uses Open Trivia database https://opentdb.com
    /// </summary>
    public class OTDBTriviaService : ITriviaService
    {
        private readonly DbService dbService;
        private readonly DiscordSocketClient discordClient;

        // holds all trivia instances on a per guild and channel basis
        private readonly Dictionary<CombinedID, OTDBTriviaInstance> trivias;

        public OTDBTriviaService(DbService db, DiscordSocketClient client)
        {
            this.dbService = db;
            this.discordClient = client;
            trivias = new Dictionary<CombinedID, OTDBTriviaInstance>();
        }

        /// <summary>
        /// Start a new trivia with the specified amount of questions in the specified Discord Channel
        /// Returns boolean success
        /// </summary>
        /// <param name="questionsToPlay">Amount of questions to play</param>
        /// <param name="guildID">Id of the Discord Guild</param>
        /// <param name="channelID">Id of the Discord channel where the trivia is played</param>
        /// <returns>success</returns>
        public async Task<bool> StartTriviaAsync(int questionsToPlay, ulong guildID, ulong channelID)
        {
            // Create a combination of guildID and channelID to form a unique identifier for each trivia instance
            CombinedID id = new CombinedID(guildID, channelID, null);
            if (!trivias.ContainsKey(id))
            {
                trivias.Add(id, new OTDBTriviaInstance(discordClient, dbService, guildID, channelID));
            }

            return await trivias[id].StartTriviaAsync(questionsToPlay);
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
            return trivias.ContainsKey(id) ? await trivias[id].SkipQuestionAsync() : false;
        }

        /// <summary>
        /// Stops the trivia in the specified guild's channel if a trivia is running, otherwise returns false
        /// </summary>
        /// <param name="guildID">Id of the Discord Guild</param>
        /// <param name="channelID">Id of the Discord channel where the trivia is played</param>
        /// <returns>success</returns>
        public async Task<bool> StopTriviaAsync(ulong guildID, ulong channelID)
        {
            // Create a combination of guildID and channelID to form a unique identifier to retrieve the trivia instance
            CombinedID id = new CombinedID(guildID, channelID, null);
            if (!trivias.ContainsKey(id))
                return false;
            else
            {
                var result = await trivias[id].StopTriviaAsync();
                trivias.Remove(id);
                return result;
            }
        }
    }
}