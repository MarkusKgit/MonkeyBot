using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using MonkeyBot.Common;
using MonkeyBot.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [Name("Info")]
    public class InfoModule : ModuleBase
    {
        private DbService db;

        public InfoModule(IServiceProvider provider)
        {
            db = provider.GetService<DbService>();
        }

        [Command("Rules")]
        [Remarks("The bot replies with the server rules in a PM")]
        [RequireContext(ContextType.Guild)]
        public async Task ListRulesAsync()
        {
            using (var uow = db.UnitOfWork)
            {
                var rules = (await uow.GuildConfigs.GetAsync(Context.Guild.Id)).Rules;
                if (rules == null || rules.Count < 1)
                    await ReplyAsync("No rules set!");
                else
                    await Context.User.SendMessageAsync(string.Join(Environment.NewLine, rules));
            }
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