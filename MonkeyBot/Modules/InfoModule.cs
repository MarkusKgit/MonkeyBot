using Discord;
using Discord.Commands;
using MonkeyBot.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [Name("Info")]
    public class InfoModule : ModuleBase
    {
        [Command("Rules")]
        [Remarks("The bot replies with the server rules in a PM")]
        [RequireContext(ContextType.Guild)]
        public async Task ListRulesAsync()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("**Our rules:**");
            builder.AppendLine("1) Do not be a troll. Be gentle, kind, funny and good to each other");
            builder.AppendLine("2) Do not spam in the text or voice chat");
            builder.AppendLine("3) Be respectful and do not disrespect others");
            builder.AppendLine("4) Do not mention cookies :cookie:");
            await Context.User.SendMessageAsync(builder.ToString());
        }

        [Command("games")]
        [Remarks("Lists all games roles and the users who have these roles")]
        [RequireContext(ContextType.Guild)]
        public async Task ListGamesAsync()
        {
            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = "These are the are all the game roles and the users assigned to them:"
            };
            // Get the role of the bot with permission manage roles
            IRole botRole = await Helpers.GetManageRolesRoleAsync(Context);
            // Get all roles that are lower than the bot's role (roles the bot can assign)
            var guildUsers = await Context.Guild.GetUsersAsync();
            foreach (var role in Context.Guild.Roles)
            {
                if (role.IsMentionable && role.Name != "everyone" && botRole?.Position > role.Position)
                {
                    var roleUsers = guildUsers.Where(x => x.RoleIds.Contains(role.Id)).Select(x => x.Username).OrderBy(x => x);
                    builder.AddField(x =>
                    {
                        x.Name = role.Name;
                        x.Value = string.Join(", ", roleUsers);
                        x.IsInline = false;
                    });
                }
            }
            await Context.User.SendMessageAsync("", false, builder.Build());
        }
    }
}