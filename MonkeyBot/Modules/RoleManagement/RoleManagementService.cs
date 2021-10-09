using DSharpPlus;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class RoleManagementService : IRoleManagementService
    {
        public async Task<DiscordRole> GetBotRoleAsync(DiscordUser botUser, DiscordGuild guild)
        {
            DiscordMember bot = await guild.GetMemberAsync(botUser.Id);
            return bot.Roles.FirstOrDefault(x => x.Permissions.HasPermission(Permissions.ManageRoles));
        }

        public IEnumerable<DiscordRole> GetAssignableRoles(DiscordRole botRole, DiscordGuild guild)
        {
            // Get all roles that are lower than the bot's role (roles the bot can assign)
            return guild.Roles.Values
                .Where(role => role.IsMentionable
                               && role != guild.EveryoneRole
                               && !role.Permissions.HasFlag(Permissions.Administrator)
                               && role.Position < botRole.Position);

        }
    }
}
