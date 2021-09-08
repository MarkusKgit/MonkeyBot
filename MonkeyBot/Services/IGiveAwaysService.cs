using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IGiveAwaysService
    {
        /// <summary>
        /// Start the giveaways service to regularly check for updates
        /// </summary>
        void Start();

        Task EnableForGuildAsync(ulong guildID, ulong channelID);

        Task DisableForGuildAsync(ulong guildID);
    }
}