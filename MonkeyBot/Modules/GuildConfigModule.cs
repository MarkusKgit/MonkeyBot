using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [MinPermissions(AccessLevel.ServerAdmin)]
    [Name("Guild Configuration")]
    [RequireContext(ContextType.Guild)]
    public class GuildConfigModule : ModuleBase
    {
        private DbService db;

        public GuildConfigModule(IServiceProvider provider)
        {
            db = provider.GetService<DbService>();
        }

        [Command("SetWelcomeMsg")]
        [Remarks("Sets the welcome message for new users. Can make use of %user% and %server%")]
        public async Task SetWelcomeMessage([Summary("The welcome message")][Remainder] string welcomeMsg)
        {
            if (string.IsNullOrEmpty(welcomeMsg))
            {
                await ReplyAsync("Please provide a welcome message");
                return;
            }

            using (var uow = db.UnitOfWork)
            {
                var config = await uow.GuildConfigs.GetOrCreateAsync(Context.Guild.Id);
                config.WelcomeMessageText = welcomeMsg;
                await uow.CompleteAsync();
            }
        }

        [Command("AddRule")]
        [Remarks("Adds a rule to the server.")]
        public async Task AddRule([Summary("The rule to add")][Remainder] string rule)
        {
            if (string.IsNullOrEmpty(rule))
            {
                await ReplyAsync("Please enter a rule");
                return;
            }
            using (var uow = db.UnitOfWork)
            {
                var config = await uow.GuildConfigs.GetOrCreateAsync(Context.Guild.Id);
                if (config.Rules == null)
                    config.Rules = new List<string>();
                config.Rules.Add(rule);
                await uow.CompleteAsync();
            }
        }

        [Command("RemoveRules")]
        [Remarks("Removes the rules from a server.")]
        public async Task RemoveRules()
        {
            using (var uow = db.UnitOfWork)
            {
                var config = await uow.GuildConfigs.GetOrCreateAsync(Context.Guild.Id);
                if (config.Rules != null)
                    config.Rules.Clear();
                await uow.CompleteAsync();
            }
        }
    }
}