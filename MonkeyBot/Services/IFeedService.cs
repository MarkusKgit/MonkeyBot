using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IFeedService
    {
        void Start();

        Task RunOnceAllFeedsAsync(ulong guildId);

        Task RunOnceSingleFeedAsync(ulong guildId, ulong channelId, string url, bool getLatest = false);
    }
}