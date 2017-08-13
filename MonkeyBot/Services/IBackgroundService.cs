using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IBackgroundService
    {
        void Start();

        Task RunOnceAllFeedsAsync(ulong guildId);

        Task RunOnceSingleFeedAsync(ulong guildId, ulong channelId, string url);
    }
}