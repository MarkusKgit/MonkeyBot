using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IGameSubscriptionService
    {
        /// <summary>
        /// Initialize the GameSubscriptionService to start listening for game launches
        /// </summary>
        void Initialize();

        /// <summary>
        /// Add a subscription for the specified game. Will send the provided user a message when someone launches the game
        /// </summary>
        /// <param name="gameName">Name of the game. Can be a part match, e.g. "Battlefield"</param>
        /// <param name="guildId">The guild to watch for game launches</param>
        /// <param name="userId">User that should receive the notification</param>
        /// <returns></returns>
        Task AddSubscriptionAsync(string gameName, ulong guildId, ulong userId);

        /// <summary>
        /// Remove a game subscription for the specified user
        /// </summary>
        /// <param name="gameName"></param>
        /// <param name="guildId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task RemoveSubscriptionAsync(string gameName, ulong guildId, ulong userId);
    }
}