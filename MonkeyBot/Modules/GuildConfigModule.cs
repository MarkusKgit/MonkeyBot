using Discord;
using Discord.Commands;
using MonkeyBot.Common;
using MonkeyBot.Models;
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
    public class GuildConfigModule : MonkeyModuleBase
    {
        private readonly IGuildService guildService;
        private readonly IBattlefieldNewsService bfService;

        public GuildConfigModule(IGuildService guildService, IBattlefieldNewsService bfService)
        {
            this.guildService = guildService;
            this.bfService = bfService;
        }


        [Command("SetWelcomeMessage")]
        [Remarks("Sets the welcome message for new users. Can make use of %user% and %server%")]
        [Example("!SetWelcomeMessage \"Hello %user%, welcome to %server%\"")]
        public async Task SetWelcomeMessageAsync([Summary("The welcome message")][Remainder] string welcomeMsg)
        {
            welcomeMsg = welcomeMsg.Trim('\"');
            if (welcomeMsg.IsEmpty())
            {
                _ = await ReplyAsync("Please provide a welcome message").ConfigureAwait(false);
                return;
            }

            GuildConfig config = await guildService.GetOrCreateConfigAsync(Context.Guild.Id).ConfigureAwait(false);
            config.WelcomeMessageText = welcomeMsg;
            await guildService.UpdateConfigAsync(config).ConfigureAwait(false);
            await ReplyAndDeleteAsync("Message set").ConfigureAwait(false);
        }

        [Command("SetDefaultChannel")]
        [Remarks("Sets the default channel for the guild where info will be posted")]
        [Example("!SetDefaultChannel general")]
        public async Task SetDefaultChannelAsync([Summary("The name of the default channel")][Remainder] string channelName)
        {
            ITextChannel channel = await GetTextChannelInGuildAsync(channelName.Trim('\"'), false).ConfigureAwait(false);
            GuildConfig config = await guildService.GetOrCreateConfigAsync(Context.Guild.Id).ConfigureAwait(false);
            config.DefaultChannelId = channel.Id;
            await guildService.UpdateConfigAsync(config).ConfigureAwait(false);
            await ReplyAndDeleteAsync("Channel set").ConfigureAwait(false);
        }


        [Command("SetWelcomeChannel")]
        [Remarks("Sets the channel where the welcome message will be posted")]
        [Example("!SetWelcomeChannel general")]
        public async Task SetWelcomeChannelAsync([Summary("The welcome message channel")][Remainder] string channelName)
        {
            ITextChannel channel = await GetTextChannelInGuildAsync(channelName.Trim('\"'), false).ConfigureAwait(false);
            GuildConfig config = await guildService.GetOrCreateConfigAsync(Context.Guild.Id).ConfigureAwait(false);
            config.WelcomeMessageChannelId = channel.Id;
            await guildService.UpdateConfigAsync(config).ConfigureAwait(false);
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
                _ = await ReplyAsync("Please provide a goodbye message").ConfigureAwait(false);
                return;
            }

            GuildConfig config = await guildService.GetOrCreateConfigAsync(Context.Guild.Id).ConfigureAwait(false);
            config.GoodbyeMessageText = goodbyeMsg;
            await guildService.UpdateConfigAsync(config).ConfigureAwait(false);
            await ReplyAndDeleteAsync("Message set").ConfigureAwait(false);
        }

        [Command("SetGoodbyeChannel")]
        [Remarks("Sets the channel where the Goodbye message will be posted")]
        [Example("!SetGoodbyeChannel general")]
        public async Task SetGoodbyeChannelAsync([Summary("The Goodbye message channel")][Remainder] string channelName)
        {
            ITextChannel channel = await GetTextChannelInGuildAsync(channelName.Trim('\"'), false).ConfigureAwait(false);

            GuildConfig config = await guildService.GetOrCreateConfigAsync(Context.Guild.Id).ConfigureAwait(false);
            config.GoodbyeMessageChannelId = channel.Id;
            await guildService.UpdateConfigAsync(config).ConfigureAwait(false);
            await ReplyAndDeleteAsync("Channel set").ConfigureAwait(false);
        }

        [Command("AddRule")]
        [Remarks("Adds a rule to the server.")]
        [Example("!AddRule \"You shall not pass!\"")]
        public async Task AddRuleAsync([Summary("The rule to add")][Remainder] string rule)
        {
            if (rule.IsEmpty())
            {
                _ = await ReplyAsync("Please enter a rule").ConfigureAwait(false);
                return;
            }

            GuildConfig config = await guildService.GetOrCreateConfigAsync(Context.Guild.Id).ConfigureAwait(false);
            config.Rules ??= new List<string>();
            config.Rules.Add(rule);
            await guildService.UpdateConfigAsync(config).ConfigureAwait(false);
            await ReplyAndDeleteAsync("Rule added").ConfigureAwait(false);
        }

        [Command("RemoveRules")]
        [Remarks("Removes all rules from a server.")]
        public async Task RemoveRulesAsync()
        {
            GuildConfig config = await guildService.GetOrCreateConfigAsync(Context.Guild.Id).ConfigureAwait(false);
            if (config.Rules != null)
            {
                config.Rules.Clear();
            }
            await guildService.UpdateConfigAsync(config).ConfigureAwait(false);
            await ReplyAndDeleteAsync("Rules removed").ConfigureAwait(false);
        }

        [Command("EnableBattlefieldUpdates")]
        [Remarks("Enables automated posting of Battlefield update news in provided channel")]
        [Example("!EnableBattlefieldUpdates #general")]
        public async Task EnableBattlefieldUpdatesAsync(ITextChannel channel)
        {
            if (channel == null)
            {
                _ = await ReplyAsync("Please provide a valid channel").ConfigureAwait(false);
                return;
            }
            await bfService.EnableForGuildAsync(Context.Guild.Id, channel.Id).ConfigureAwait(false);
            await ReplyAndDeleteAsync("Battlefield Updates enabled!").ConfigureAwait(false);
        }

        [Command("DisableBattlefieldUpdates")]
        [Remarks("Disables automated posting of Battlefield update news")]
        public async Task DisableBattlefieldUpdatesAsync()
        {
            await bfService.DisableForGuildAsync(Context.Guild.Id).ConfigureAwait(false);
            await ReplyAndDeleteAsync("Battlefield Updates disabled!").ConfigureAwait(false);
        }

        [Command("EnableStreamingNotifications")]
        [Remarks("Enables automated notifications of people that start streaming (if they have enabled it for themselves). Info will be posted in the default channel of the guild")]
        [Example("!EnableStreamingNotifications")]
        public async Task EnableStreamingNotificationsAsync()
        {
            await ToggleStreamingAnnouncementsAsync(true).ConfigureAwait(false);
            await ReplyAndDeleteAsync($"Streaming Notifications enabled! {Environment.NewLine}Use !AnnounceMyStreams to automatically have your streams broadcasted when you start streaming").ConfigureAwait(false);
        }

        [Command("DisableStreamingNotifications")]
        [Remarks("Disables automated notifications of people that start streaming")]
        public async Task DisableStreamingNotificationsAsync()
        {
            await ToggleStreamingAnnouncementsAsync(false).ConfigureAwait(false);
            await ReplyAndDeleteAsync($"Streaming Notifications disabled!").ConfigureAwait(false);
        }

        [Command("AnnounceMyStreams")]
        [Remarks("Enable automatic posting of your stream info when you start streaming")]
        [MinPermissions(AccessLevel.User)]
        public async Task AnnounceMyStreamsAsync()
        {
            GuildConfig config = await guildService.GetOrCreateConfigAsync(Context.Guild.Id).ConfigureAwait(false);
            if (!config.StreamAnnouncementsEnabled)
            {
                await ReplyAndDeleteAsync($"Stream broadcasting is disabled in this guild. An admin has to enable it first with !EnableStreamingNotifications").ConfigureAwait(false);
                return;
            }
            config.ConfirmedStreamerIds ??= new List<ulong>();
            config.ConfirmedStreamerIds.Add(Context.User.Id);
            await guildService.UpdateConfigAsync(config).ConfigureAwait(false);
            await ReplyAndDeleteAsync($"Your streams will now be broadcasted!").ConfigureAwait(false);
        }

        private async Task ToggleStreamingAnnouncementsAsync(bool enable)
        {
            GuildConfig config = await guildService.GetOrCreateConfigAsync(Context.Guild.Id).ConfigureAwait(false);
            config.StreamAnnouncementsEnabled = enable;
            await guildService.UpdateConfigAsync(config).ConfigureAwait(false);
        }
    }
}