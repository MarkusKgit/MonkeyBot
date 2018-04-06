using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IRoleButtonService
    {
        /// <summary>
        /// Start the RoleButtonService to watch for reactions
        /// </summary>
        void Initialize();

        /// <summary>
        /// Add a new role button link. Automatically sets up reactions and assigns a role to the user who clicks the reaction.
        /// </summary>
        /// <param name="guildId">Id of the guild to set up the link for</param>
        /// <param name="messageId">Id of the message where the reactions are added</param>
        /// <param name="roleId">Id of the role that will be assigned</param>
        /// <param name="emoteString">The emote to use</param>
        /// <returns></returns>
        Task AddRoleButtonLinkAsync(ulong guildId, ulong messageId, ulong roleId, string emoteString);

        /// <summary>
        /// Removes a role button link. Automatically removes reactions
        /// </summary>
        /// <param name="guildId">Id of the guild where to remove the link</param>
        /// <param name="messageId">Id of the message the link was set up for</param>
        /// <param name="roleId">Id of the role the link was set up for</param>
        /// <returns></returns>
        Task RemoveRoleButtonLinkAsync(ulong guildId, ulong messageId, ulong roleId);

        /// <summary>
        /// Removes all role button links in the specified guild
        /// </summary>
        /// <param name="guildId">Id of the guild where to remove the links</param>
        /// <returns></returns>
        Task RemoveAllRoleButtonLinksAsync(ulong guildId);

        /// <summary>
        /// Check if the specified link exists
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="messageId"></param>
        /// <param name="roleId"></param>
        /// <param name="emoteString"></param>
        /// <returns></returns>
        Task<bool> ExistsAsync(ulong guildId, ulong messageId, ulong roleId, string emoteString = "");

        /// <summary>
        /// List all links for the specified guild
        /// </summary>
        /// <param name="guildId">Id of the guild to get the links for</param>
        /// <returns></returns>
        Task<string> ListAllAsync(ulong guildId);
    }
}