using MonkeyBot.Common;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    /// <summary>
    /// Basic functionality of a service that can manage trivias on a per guild and channel basis
    /// </summary>
    public interface ITriviaService
    {
        /// <summary>
        /// Start a new trivia with the specified amount of questions in the specified Discord Channel
        /// Returns boolean success
        /// </summary>
        /// <param name="guildId">Id of the guild where the trivia will be started</param>
        /// <param name="channelId">Id of the channel where the trivia will be started</param>
        /// <param name="questionsToPlay">Amount of questions to play</param>
        /// <returns>success</returns>
        Task<bool> StartTriviaAsync(ulong guildId, ulong channelId, int questionsToPlay);

        /// <summary>
        /// Stops the trivia in the specified guild's channel if a trivia is running, otherwise returns false
        /// </summary>
        /// <param name="guildId">Id of the guild where the trivia is running</param>
        /// <param name="channelId">Id of the channel where the trivia is running</param>
        /// <returns>success</returns>
        Task<bool> StopTriviaAsync(ulong guildId, ulong channelId);

        /// <summary>
        /// Gets the current global high scores for the guild
        /// </summary> 
        /// <param name="guildId">Id of the guild for which the high score is requested</param>
        /// <param name="amount">Max. number of scores to get</param>
        /// <returns>List of users with associated score</returns>
        Task<IEnumerable<(ulong userId, int score)>> GetGlobalHighScoresAsync(ulong guildId, int amount);
    }
}