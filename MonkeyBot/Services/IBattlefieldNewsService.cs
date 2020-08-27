using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IBattlefieldNewsService
    {
        /// <summary>
        /// Start the news service to regularly check for updates
        /// </summary>
        void Start();

        Task EnableForGuildAsync(ulong guildID, ulong channelID);

        Task DisableForGuildAsync(ulong guildID);
    }
}
