using System.Net;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IGameServerService
    {
        Task AddServerAsync(IPEndPoint endpoint, ulong guildID, ulong channelID);

        Task RemoveServerAsync(IPEndPoint endPoint, ulong guildID);

        void Initialize();
    }
}