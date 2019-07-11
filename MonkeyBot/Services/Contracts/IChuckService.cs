using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IChuckService
    {
        /// <summary>
        /// Get a random chuck norris fact
        /// </summary>
        /// <returns></returns>
        Task<string> GetChuckFactAsync();

        /// <summary>
        /// Get a random chuck norris fact and replace "Chuck Norris" with the provided name
        /// </summary>
        /// <param name="userName">The name that replaces "Chuck Norris" in this fact</param>
        /// <returns></returns>
        Task<string> GetChuckFactAsync(string userName);
    }
}