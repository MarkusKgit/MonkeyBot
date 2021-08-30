using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MonkeyBot.Common;
using MonkeyBot.Models;
using MonkeyBot.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [MinPermissions(AccessLevel.ServerAdmin)]
    [Description("Guild Configuration")]
    [RequireGuild]
    public class GuildModule : BaseCommandModule
    {
        private readonly IGuildService _guildService;
        private readonly IBattlefieldNewsService _bfService;

        public GuildModule(IGuildService guildService, IBattlefieldNewsService bfService)
        {
            _guildService = guildService;
            _bfService = bfService;
        }

        [Command("SetDefaultChannel")]
        [Description("Sets the default channel for the guild where info will be posted")]
        [Example("!SetDefaultChannel general")]
        public async Task SetDefaultChannelAsync(CommandContext ctx, [Description("The channel which should become the default")][RemainingText] DiscordChannel channel)
        {
            GuildConfig config = await _guildService.GetOrCreateConfigAsync(ctx.Guild.Id);
            config.DefaultChannelId = channel.Id;
            await _guildService.UpdateConfigAsync(config);
            _ = await ctx.OkAsync("Default channel set");
        }

        [Command("SetWelcomeMessage")]
        [Description("Sets the welcome message for new users. Can make use of %user% and %server%")]
        [Example("!SetWelcomeMessage \"Hello %user%, welcome to %server%\"")]
        public async Task SetWelcomeMessageAsync(CommandContext ctx, [RemainingText, Description("The welcome message")] string welcomeMsg)
        {
            welcomeMsg = welcomeMsg.Trim('\"');
            if (welcomeMsg.IsEmpty())
            {
                _ = await ctx.RespondAsync("Please provide a welcome message");
                return;
            }

            GuildConfig config = await _guildService.GetOrCreateConfigAsync(ctx.Guild.Id);
            config.WelcomeMessageText = welcomeMsg;
            await _guildService.UpdateConfigAsync(config);
            _ = await ctx.OkAsync("Welcome Message set");
        }

        [Command("SetWelcomeChannel")]
        [Description("Sets the channel where the welcome message will be posted")]
        [Example("!SetWelcomeChannel general")]
        public async Task SetWelcomeChannelAsync(CommandContext ctx, [Description("The channel where the welcome message should be posted")] DiscordChannel channel)
        {
            GuildConfig config = await _guildService.GetOrCreateConfigAsync(ctx.Guild.Id);
            config.WelcomeMessageChannelId = channel.Id;
            await _guildService.UpdateConfigAsync(config);
            _ = await ctx.OkAsync("Welcome channel set");
        }

        [Command("SetGoodbyeMessage")]
        [Description("Sets the Goodbye message for new users. Can make use of %user% and %server%")]
        [Example("!SetGoodbyeMessage \"Goodbye %user%, farewell!\"")]
        public async Task SetGoodbyeMessageAsync(CommandContext ctx, [RemainingText, Description("The Goodbye message")] string goodbyeMsg)
        {
            goodbyeMsg = goodbyeMsg.Trim('\"');
            if (goodbyeMsg.IsEmpty())
            {
                _ = await ctx.ErrorAsync("Please provide a goodbye message");
                return;
            }

            GuildConfig config = await _guildService.GetOrCreateConfigAsync(ctx.Guild.Id);
            config.GoodbyeMessageText = goodbyeMsg;
            await _guildService.UpdateConfigAsync(config);
            _ = await ctx.OkAsync("Goodbye Message set");
        }

        [Command("SetGoodbyeChannel")]
        [Description("Sets the channel where the Goodbye message will be posted")]
        [Example("!SetGoodbyeChannel general")]
        public async Task SetGoodbyeChannelAsync(CommandContext ctx, [Description("The channel where the goodbye message should be posted")] DiscordChannel channel)
        {
            GuildConfig config = await _guildService.GetOrCreateConfigAsync(ctx.Guild.Id);
            config.GoodbyeMessageChannelId = channel.Id;
            await _guildService.UpdateConfigAsync(config);
            _ = await ctx.OkAsync("Goodbye Channel set");
        }

        [Command("AddRule")]
        [Description("Adds a rule to the server.")]
        [Example("!AddRule \"You shall not pass!\"")]
        public async Task AddRuleAsync(CommandContext ctx, [RemainingText, Description("The rule to add")] string rule)
        {
            if (rule.IsEmpty())
            {
                _ = await ctx.ErrorAsync("Please provide a rule");
                return;
            }

            GuildConfig config = await _guildService.GetOrCreateConfigAsync(ctx.Guild.Id);
            config.Rules ??= new List<string>();
            config.Rules.Add(rule);
            await _guildService.UpdateConfigAsync(config);
            _ = await ctx.OkAsync("Rule added");
        }

        [Command("RemoveRules")]
        [Description("Removes all rules from a server.")]
        public async Task RemoveRulesAsync(CommandContext ctx)
        {
            GuildConfig config = await _guildService.GetOrCreateConfigAsync(ctx.Guild.Id);
            if (config.Rules != null)
            {
                config.Rules.Clear();
            }
            await _guildService.UpdateConfigAsync(config);
            _ = await ctx.OkAsync("All rules removed");
        }

        [Command("EnableBattlefieldUpdates")]
        [Description("Enables automated posting of Battlefield update news in provided channel")]
        [Example("!EnableBattlefieldUpdates #general")]
        public async Task EnableBattlefieldUpdatesAsync(CommandContext ctx, [Description("The channel where the Battlefield updates should be posted")] DiscordChannel channel)
        {
            await _bfService.EnableForGuildAsync(ctx.Guild.Id, channel.Id);
            _ = await ctx.OkAsync("Battlefield Updates enabled!");
        }

        [Command("DisableBattlefieldUpdates")]
        [Description("Disables automated posting of Battlefield update news")]
        public async Task DisableBattlefieldUpdatesAsync(CommandContext ctx)
        {
            await _bfService.DisableForGuildAsync(ctx.Guild.Id);
            _ = await ctx.OkAsync("Battlefield Updates disabled!");
        }

        [Command("EnableStreamingNotifications")]
        [Description("Enables automated notifications of people that start streaming (if they have enabled it for themselves). Info will be posted in the default channel of the guild")]
        [Example("!EnableStreamingNotifications")]
        public async Task EnableStreamingNotificationsAsync(CommandContext ctx)
        {
            await ToggleStreamingAnnouncementsAsync(ctx.Guild.Id, true);
            _ = await ctx.OkAsync($"Streaming Notifications enabled! {Environment.NewLine}Use {ctx.Prefix}AnnounceMyStreams to automatically have your streams broadcasted when you start streaming");
        }

        [Command("DisableStreamingNotifications")]
        [Description("Disables automated notifications of people that start streaming")]
        public async Task DisableStreamingNotificationsAsync(CommandContext ctx)
        {
            await ToggleStreamingAnnouncementsAsync(ctx.Guild.Id, false);
            _ = await ctx.OkAsync($"Streaming Notifications disabled!");
        }

        [Command("AnnounceMyStreams")]
        [Description("Enable automatic posting of your stream info when you start streaming")]
        [MinPermissions(AccessLevel.User)]
        public async Task AnnounceMyStreamsAsync(CommandContext ctx)
        {
            GuildConfig config = await _guildService.GetOrCreateConfigAsync(ctx.Guild.Id);
            if (!config.StreamAnnouncementsEnabled)
            {
                await ctx.ErrorAsync($"Stream broadcasting is disabled in this guild. An admin has to enable it first with {ctx.Prefix}EnableStreamingNotifications");
                return;
            }
            config.ConfirmedStreamerIds ??= new List<ulong>();
            config.ConfirmedStreamerIds.Add(ctx.User.Id);
            await _guildService.UpdateConfigAsync(config);
            _ = await ctx.OkAsync($"Your streams will now be announced!");
        }

        private async Task ToggleStreamingAnnouncementsAsync(ulong guildId, bool enable)
        {
            GuildConfig config = await _guildService.GetOrCreateConfigAsync(guildId);
            config.StreamAnnouncementsEnabled = enable;
            await _guildService.UpdateConfigAsync(config);
        }
    }
}