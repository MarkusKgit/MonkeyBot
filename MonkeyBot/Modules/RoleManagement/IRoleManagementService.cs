using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IRoleManagementService
    {
        IEnumerable<DiscordRole> GetAssignableRoles(DiscordRole botRole, DiscordGuild guild);
        Task<DiscordRole> GetBotRoleAsync(DiscordUser botUser, DiscordGuild guild);
    }
}