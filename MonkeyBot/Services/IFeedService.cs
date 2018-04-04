using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IFeedService
    {
        void Start();

        Task AddFeedAsync(string url, ulong guildId, ulong channelId);

        Task RemoveFeedAsync(string url, ulong guildId, ulong channelId);

        Task RemoveAllFeedsAsync(ulong guildId, ulong? channelId);

        Task<List<string>> GetFeedUrlsForGuildAsync(ulong guildId, ulong? channelId = null);
    }
}