using Discord.Commands;
using MonkeyBot.Common;
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
        /// <param name="questionsToPlay">Amount of questions to play</param>        
        /// <returns>success</returns>
        Task<bool> StartTriviaAsync(int questionsToPlay, SocketCommandContext context);

        /// <summary>
        /// Skips the trivia in the specified guild's channel if a trivia is running, otherwise returns false
        /// </summary>
        /// <returns>success</returns>
        Task<bool> SkipQuestionAsync(DiscordId id);

        /// <summary>
        /// Stops the trivia in the specified guild's channel if a trivia is running, otherwise returns false
        /// </summary>
        /// <returns>success</returns>
        Task<bool> StopTriviaAsync(DiscordId id);
    }
}