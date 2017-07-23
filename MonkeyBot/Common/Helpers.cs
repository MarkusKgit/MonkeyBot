using Discord;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Common
{
    public static class Helpers
    {
        /// <summary>Get the role of the bot with permission Manage Roles</summary>
        public static async Task<IRole> GetBotRoleAsync(ICommandContext context)
        {
            var thisBot = await context.Guild.GetUserAsync(context.Client.CurrentUser.Id);
            var ownrole = context.Guild.Roles.Where(x => x.Permissions.ManageRoles == true && x.Id == thisBot.RoleIds.Max()).FirstOrDefault();
            return ownrole;
        }
    }
}
