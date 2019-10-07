using Discord.Commands;
using MonkeyBot.Common;
using MonkeyBot.Database;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    /// <summary>
    /// Service that handles Trivias on a per guild and channel basis
    /// Uses Open Trivia database https://opentdb.com
    /// </summary>
    public class OTDBTriviaService : ITriviaService
    {
        private readonly MonkeyDBContext dbContext;

        // holds all trivia instances on a per guild and channel basis
        private readonly ConcurrentDictionary<DiscordId, OTDBTriviaInstance> trivias;

        public OTDBTriviaService(MonkeyDBContext dbContext)
        {
            this.dbContext = dbContext;
            trivias = new ConcurrentDictionary<DiscordId, OTDBTriviaInstance>();
        }


        /// <summary>
        /// Start a new trivia with the specified amount of questions in the specified Discord Channel
        /// Returns boolean success
        /// </summary>
        /// <param name="questionsToPlay">Amount of questions to play</param>
        /// <param name="context">Message context of the channel where the trivia should be hosted</param>
        /// <returns>success</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
        public async Task<bool> StartTriviaAsync(int questionsToPlay, SocketCommandContext context)
        {
            // Create a combination of guildID and channelID to form a unique identifier for each trivia instance
            var id = new DiscordId(context.Guild.Id, context.Channel.Id, null);
            if (!trivias.ContainsKey(id))
            {
                _ = trivias.TryAdd(id, new OTDBTriviaInstance(context, dbContext));
            }
            return trivias.TryGetValue(id, out OTDBTriviaInstance instance)
                ? await instance.StartTriviaAsync(questionsToPlay).ConfigureAwait(false)
                : false;
        }

        /// <summary>
        /// Skips the trivia in the specified guild's channel if a trivia is running, otherwise returns false
        /// </summary>
        /// <param name="id">Combined Id of the Discord Guild and channel for the trivia</param>
        /// <returns>success</returns>
        public async Task<bool> SkipQuestionAsync(DiscordId id) 
            => trivias.ContainsKey(id) ? await trivias[id].SkipQuestionAsync().ConfigureAwait(false) : false;

        /// <summary>
        /// Stops the trivia in the specified guild's channel if a trivia is running, otherwise returns false
        /// </summary>
        /// <param name="id">Combined Id of the Discord Guild and channel for the trivia</param>
        /// <returns>success</returns>
        public async Task<bool> StopTriviaAsync(DiscordId id)
        {
            if (!trivias.ContainsKey(id))
            {
                return false;
            }
            else
            {
                bool result = await trivias[id].StopTriviaAsync().ConfigureAwait(false);
                if (trivias.TryRemove(id, out OTDBTriviaInstance instance))
                {
                    instance.Dispose();
                }

                return result;
            }
        }

        /// <summary>
        /// Gets the current global high scores for the guild
        /// </summary>
        /// <param name="context">Context of the channel where the high score was requested</param>
        /// <returns></returns>
        public async Task<string> GetGlobalHighScoresAsync(int amount, SocketCommandContext context)
        {
            var id = new DiscordId(context.Guild.Id, context.Channel.Id, null);
            if (id.GuildId == null || !trivias.ContainsKey(id))
            {
                using var trivia = new OTDBTriviaInstance(context, dbContext);
                return await trivia.GetGlobalHighScoresAsync(amount, id.GuildId.Value).ConfigureAwait(false);
            }
            else
            {
                return await trivias[id].GetGlobalHighScoresAsync(amount, id.GuildId.Value).ConfigureAwait(false);
            }
        }
    }
}