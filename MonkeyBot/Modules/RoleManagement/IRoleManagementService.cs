using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IRoleManagementService
    {
        /// <summary>
        /// Get all the roles in the specified guild that the Bot can assign
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        Task<IEnumerable<DiscordRole>> GetAssignableRolesAsync(DiscordGuild guild);

        /// <summary>
        /// Get the role of the Bot with the permission to assign roles
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        Task<DiscordRole> GetBotRoleAsync(DiscordGuild guild);
    }
}