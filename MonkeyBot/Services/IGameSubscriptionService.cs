using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IGameSubscriptionService
    {
        void Initialize();

        Task AddSubscriptionAsync(string gameName, ulong guildId, ulong userId);

        Task RemoveSubscriptionAsync(string gameName, ulong guildId, ulong userId);
    }
}