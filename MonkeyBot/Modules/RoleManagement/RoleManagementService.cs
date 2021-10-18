using DSharpPlus;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class RoleManagementService : IRoleManagementService
    {
        private readonly DiscordClient _discordClient;

        public RoleManagementService(DiscordClient discordClient)
        {
            _discordClient = discordClient;
        }

        public async Task<DiscordRole> GetBotRoleAsync(DiscordGuild guild)
        {
            var botUser = _discordClient.CurrentUser;
            DiscordMember bot = await guild.GetMemberAsync(botUser.Id);
            return bot.Roles.FirstOrDefault(x => x.Permissions.HasPermission(Permissions.ManageRoles));
        }

        public async Task<IEnumerable<DiscordRole>> GetAssignableRolesAsync(DiscordGuild guild)
        {
            DiscordRole botRole = await GetBotRoleAsync(guild);
            // Get all roles that are lower than the bot's role (roles the bot can assign)
            return guild.Roles.Values
                .Where(role => role.IsMentionable
                               && role != guild.EveryoneRole
                               && !role.Permissions.HasFlag(Permissions.Administrator)
                               && role.Position < botRole.Position);

        }
    }
}
