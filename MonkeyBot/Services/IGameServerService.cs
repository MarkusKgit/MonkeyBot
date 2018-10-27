using System.Net;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IGameServerService
    {
        /// <summary>
        /// Adds a game server listener to the specified channel
        /// </summary>
        /// <param name="endpoint">IP:ListenPort of the gameserver</param>
        /// <param name="guildID"></param>
        /// <param name="channelID"></param>
        /// <returns></returns>
        Task<bool> AddServerAsync(IPEndPoint endpoint, ulong guildID, ulong channelID);

        /// <summary>
        /// Removes a game server listener from the specified channel
        /// </summary>
        /// <param name="endpoint">IP:ListenPort of the gameserver</param>
        /// <param name="guildID"></param>
        /// <returns></returns>
        Task RemoveServerAsync(IPEndPoint endPoint, ulong guildID);

        /// <summary>
        /// Start the GameServerService to regularly check for game server updates
        /// </summary>
        void Initialize();
    }
}