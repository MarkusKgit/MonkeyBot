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
        /// <param name="guildID">Id of the Discord Guild</param>
        /// <param name="channelID">Id of the Discord channel where the trivia is played</param>
        /// <returns>success</returns>
        Task<bool> StartTriviaAsync(int questionsToPlay, ulong guildID, ulong channelID);

        /// <summary>
        /// Skips the trivia in the specified guild's channel if a trivia is running, otherwise returns false
        /// </summary>
        /// <param name="guildID">Id of the Discord Guild</param>
        /// <param name="channelID">Id of the Discord channel where the trivia is played</param>
        /// <returns>success</returns>
        Task<bool> SkipQuestionAsync(ulong guildID, ulong channelID);

        /// <summary>
        /// Stops the trivia in the specified guild's channel if a trivia is running, otherwise returns false
        /// </summary>
        /// <param name="guildID">Id of the Discord Guild</param>
        /// <param name="channelID">Id of the Discord channel where the trivia is played</param>
        /// <returns>success</returns>
        Task<bool> StopTriviaAsync(ulong guildID, ulong channelID);
    }
}