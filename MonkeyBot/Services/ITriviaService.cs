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
        Task<bool> StartAsync(int questionsToPlay, ulong guildID, ulong channelID);

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
        Task<bool> StopAsync(ulong guildID, ulong channelID);

        /// <summary>
        /// Returns a formated string that contains the specified amount of high scores in the specified guild
        /// </summary>
        /// <param name="count">max number of high scores to get</param>
        /// <param name="guildID">Id of the Discord Guild</param>
        /// <returns></returns>
        Task<string> GetAllTimeHighScoresAsync(int Count, ulong guildID);
    }
}