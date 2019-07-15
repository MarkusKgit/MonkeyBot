using Discord;
using Discord.Commands;
using dokas.FluentStrings;
using Microsoft.EntityFrameworkCore;
using MonkeyBot.Common;
using MonkeyBot.Database;
using MonkeyBot.Models;
using MonkeyBot.Preconditions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [MinPermissions(AccessLevel.ServerAdmin)]
    [Name("Guild Configuration")]
    [RequireContext(ContextType.Guild)]
    public class GuildConfigModule : MonkeyModuleBase
    {
        private readonly MonkeyDBContext dbContext;

        public GuildConfigModule(MonkeyDBContext dbContext)
        {
            this.dbContext = dbContext;
        }


        [Command("SetWelcomeMessage")]
        [Remarks("Sets the welcome message for new users. Can make use of %user% and %server%")]
        [Example("!SetWelcomeMessage \"Hello %user%, welcome to %server%\"")]
        public async Task SetWelcomeMessageAsync([Summary("The welcome message")][Remainder] string welcomeMsg)
        {
            welcomeMsg = welcomeMsg.Trim('\"');
            if (welcomeMsg.IsEmpty())
            {
                await ReplyAsync("Please provide a welcome message").ConfigureAwait(false);
                return;
            }

            GuildConfig config = await GetOrCreatConfigAsync(Context.Guild.Id).ConfigureAwait(false);
            config.WelcomeMessageText = welcomeMsg;
            dbContext.GuildConfigs.Update(config);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            await ReplyAndDeleteAsync("Message set").ConfigureAwait(false);
        }
                

        [Command("SetWelcomeChannel")]
        [Remarks("Sets the channel where the welcome message will be posted")]
        [Example("!SetWelcomeChannel general")]
        public async Task SetWelcomeChannelAsync([Summary("The welcome message channel")][Remainder] string channelName)
        {            
            ITextChannel channel = await GetTextChannelInGuildAsync(channelName.Trim('\"'), false).ConfigureAwait(false);
            GuildConfig config = await GetOrCreatConfigAsync(Context.Guild.Id).ConfigureAwait(false);
            config.WelcomeMessageChannelId = channel.Id;
            dbContext.GuildConfigs.Update(config);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            await ReplyAndDeleteAsync("Channel set").ConfigureAwait(false);
        }

        [Command("SetGoodbyeMessage")]
        [Remarks("Sets the Goodbye message for new users. Can make use of %user% and %server%")]
        [Example("!SetGoodbyeMessage \"Goodbye %user%, farewell!\"")]
        public async Task SetGoodbyeMessageAsync([Summary("The Goodbye message")][Remainder] string goodbyeMsg)
        {
            goodbyeMsg = goodbyeMsg.Trim('\"');
            if (goodbyeMsg.IsEmpty())
            {
                await ReplyAsync("Please provide a goodbye message").ConfigureAwait(false);
                return;
            }

            GuildConfig config = await GetOrCreatConfigAsync(Context.Guild.Id).ConfigureAwait(false);
            config.GoodbyeMessageText = goodbyeMsg;
            dbContext.GuildConfigs.Update(config);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            await ReplyAndDeleteAsync("Message set").ConfigureAwait(false);
        }

        [Command("SetGoodbyeChannel")]
        [Remarks("Sets the channel where the Goodbye message will be posted")]
        [Example("!SetGoodbyeChannel general")]
        public async Task SetGoodbyeChannelAsync([Summary("The Goodbye message channel")][Remainder] string channelName)
        {
            ITextChannel channel = await GetTextChannelInGuildAsync(channelName.Trim('\"'), false).ConfigureAwait(false);

            GuildConfig config = await GetOrCreatConfigAsync(Context.Guild.Id).ConfigureAwait(false);
            config.GoodbyeMessageChannelId = channel.Id;
            dbContext.GuildConfigs.Update(config);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            await ReplyAndDeleteAsync("Channel set").ConfigureAwait(false);
        }
                        
        [Command("AddRule")]
        [Remarks("Adds a rule to the server.")]
        [Example("!AddRule \"You shall not pass!\"")]
        public async Task AddRuleAsync([Summary("The rule to add")][Remainder] string rule)
        {
            if (rule.IsEmpty())
            {
                await ReplyAsync("Please enter a rule").ConfigureAwait(false);
                return;
            }
            
            GuildConfig config = await GetOrCreatConfigAsync(Context.Guild.Id).ConfigureAwait(false);
            if (config.Rules == null)
                config.Rules = new List<string>();
            config.Rules.Add(rule);
            dbContext.GuildConfigs.Update(config);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            await ReplyAndDeleteAsync("Rule added").ConfigureAwait(false);
        }

        [Command("RemoveRules")]
        [Remarks("Removes all rules from a server.")]
        public async Task RemoveRulesAsync()
        {
            GuildConfig config = await GetOrCreatConfigAsync(Context.Guild.Id).ConfigureAwait(false);
            if (config.Rules != null)
                config.Rules.Clear();
            dbContext.GuildConfigs.Update(config);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            await ReplyAndDeleteAsync("Rules removed").ConfigureAwait(false);
        }

        private async Task<GuildConfig> GetOrCreatConfigAsync(ulong guildId)
        {
            return (await dbContext.GuildConfigs.SingleOrDefaultAsync(c => c.GuildID == guildId).ConfigureAwait(false)) ?? new GuildConfig { GuildID = Context.Guild.Id };
        }
    }
}