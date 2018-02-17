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
                var rules = (await uow.GuildConfigs.GetAsync(Context.Guild.Id))?.Rules;
                if (rules == null || rules.Count < 1)
                    await ReplyAsync("No rules set!");
                else
                {
                    var builder = new EmbedBuilder();
                    builder.Color = new Color(255, 0, 0);
                    builder.AddField($"Rules of {Context.Guild.Name}:", string.Join(Environment.NewLine, rules));
                    await Context.User.SendMessageAsync("", false, builder.Build());
                }
            }
        }
    }
}