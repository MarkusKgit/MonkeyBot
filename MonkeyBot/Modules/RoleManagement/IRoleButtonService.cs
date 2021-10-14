using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IRoleButtonService
    {
        /// <summary>
        /// Start the RoleButtonService to watch for reactions
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// List all links for the specified guild
        /// </summary>
        /// <param name="guildId">Id of the guild to get the links for</param>
        /// <returns></returns>
        Task<string> ListAllAsync(ulong guildId);

        /// <summary>
        /// Check if the specified link exists
        /// </summary>
        /// <param name="guildID">Id of the guild where the message lies</param>
        /// <returns></returns>
        Task<bool> ExistsAsync(ulong guildID);

        /// <summary>
        /// Add a new role selector dropdown. Assigns the selected role to the user who made the selection.
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="channelId"></param>
        /// <param name="messageId"></param>
        /// <returns></returns>
        Task AddRoleSelectorComponentAsync(ulong guildId, ulong channelId, ulong messageId, DiscordUser botUser);

        /// <summary>
        /// Removes all role selecor links in the specified guild
        /// </summary>
        /// <param name="guildId">Id of the guild where to remove the links</param>
        /// <returns></returns>
        Task RemoveAllRoleSelectorComponentsAsync(ulong guildId);

        /// <summary>
        /// Removes a role selector link.
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="messageId"></param>
        /// <returns></returns>
        Task RemoveRoleSelectorComponentsAsync(ulong guildId, ulong channelId, ulong messageId);
    }
}