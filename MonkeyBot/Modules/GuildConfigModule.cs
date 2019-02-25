using Discord;
using Discord.Commands;
using dokas.FluentStrings;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [MinPermissions(AccessLevel.ServerAdmin)]
    [Name("Guild Configuration")]
    [RequireContext(ContextType.Guild)]
    public class GuildConfigModule : MonkeyModuleBase
    {
        private readonly DbService dbService;

        public GuildConfigModule(DbService db)
        {
            this.dbService = db;
        }

        #region WelcomeMessage

        [Command("SetWelcomeMessage")]
        [Remarks("Sets the welcome message for new users. Can make use of %user% and %server%")]
        [Example("!SetWelcomeMessage \"Hello %user%, welcome to %server%\"")]
        public async Task SetWelcomeMessageAsync([Summary("The welcome message")][Remainder] string welcomeMsg)
        {
            welcomeMsg = welcomeMsg.Trim('\"');
            if (welcomeMsg.IsEmpty())
            {
                await ReplyAsync("Please provide a welcome message");
                return;
            }

            using (var uow = dbService.UnitOfWork)
            {
                var config = await uow.GuildConfigs.GetAsync(Context.Guild.Id);
                if (config == null)
                    config = new GuildConfig(Context.Guild.Id);
                config.WelcomeMessageText = welcomeMsg;
                await uow.GuildConfigs.AddOrUpdateAsync(config);
                await uow.CompleteAsync();
            }
            await ReplyAndDeleteAsync("Message set");
        }

        [Command("SetWelcomeChannel")]
        [Remarks("Sets the channel where the welcome message will be posted")]
        [Example("!SetWelcomeChannel general")]
        public async Task SetWelcomeChannelAsync([Summary("The welcome message channel")][Remainder] string channelName)
        {            
            ITextChannel channel = await GetTextChannelInGuildAsync(channelName.Trim('\"'), false);
            using (var uow = dbService.UnitOfWork)
            {
                var config = await uow.GuildConfigs.GetAsync(Context.Guild.Id);
                if (config == null)
                    config = new GuildConfig(Context.Guild.Id);
                config.WelcomeMessageChannelId = channel.Id;
                await uow.GuildConfigs.AddOrUpdateAsync(config);
                await uow.CompleteAsync();
            }
            await ReplyAndDeleteAsync("Channel set");
        }

        #endregion WelcomeMessage

        #region Rules

        [Command("AddRule")]
        [Remarks("Adds a rule to the server.")]
        [Example("!AddRule \"You shall not pass!\"")]
        public async Task AddRuleAsync([Summary("The rule to add")][Remainder] string rule)
        {
            if (rule.IsEmpty())
            {
                await ReplyAsync("Please enter a rule");
                return;
            }
            using (var uow = dbService.UnitOfWork)
            {
                var config = await uow.GuildConfigs.GetAsync(Context.Guild.Id);
                if (config == null)
                    config = new GuildConfig(Context.Guild.Id);
                config.Rules.Add(rule);
                await uow.GuildConfigs.AddOrUpdateAsync(config);
                await uow.CompleteAsync();
            }
            await ReplyAndDeleteAsync("Rule added");
        }

        [Command("RemoveRules")]
        [Remarks("Removes all rules from a server.")]
        public async Task RemoveRulesAsync()
        {
            using (var uow = dbService.UnitOfWork)
            {
                var config = await uow.GuildConfigs.GetAsync(Context.Guild.Id);
                if (config != null)
                {
                    config.Rules.Clear();
                    await uow.GuildConfigs.AddOrUpdateAsync(config);
                    await uow.CompleteAsync();
                }
            }
            await ReplyAndDeleteAsync("Rules removed");
        }

        #endregion Rules
    }
}