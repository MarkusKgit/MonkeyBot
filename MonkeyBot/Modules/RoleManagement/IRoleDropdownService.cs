using MonkeyBot.Models;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IRoleDropdownService
    {
        /// <summary>
        /// Start the <see cref="IRoleDropdownService"/> to watch for component interactions
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Get the <see cref="MessageComponentLink"/> for the specified guild
        /// </summary>
        /// <param name="guildId">Id of the guild to get the link</param>
        /// <returns><see cref="MessageComponentLink"/> if found, otherwise null</returns>
        Task<MessageComponentLink> GetForGuildAsync(ulong guildId);

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
        /// <exception cref="ArgumentException">Thrown when Guild, Channel or Message are null</exception>
        /// <exception cref="MessageComponentLinkAlreadyExistsException">Thrown when a message component link already exists in the guild</exception>
        Task AddRoleSelectorComponentAsync(ulong guildId, ulong channelId, ulong messageId);

        /// <summary>
        /// Removes the role selector link in the specified guild.
        /// </summary>
        /// <param name="guildId">Id of the guild where the role selector link should be removed</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Thrown when Guild is null</exception>
        /// <exception cref="MessageComponentLinkNotFoundException">Thrown when message component link does not exist</exception>
        Task RemoveRoleSelectorComponentsAsync(ulong guildId);
    }
}