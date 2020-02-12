using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using MonkeyBot.Models;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class MonkeyClient : DiscordSocketClient
    {
        private readonly ILogger<MonkeyClient> logger;
        private readonly IGuildService guildService;

        private static readonly DiscordSocketConfig discordConfig = new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Warning,
            MessageCacheSize = 1000
        };

        public MonkeyClient(IGuildService guildService, ILogger<MonkeyClient> logger) : base(discordConfig)
        {
            this.logger = logger;
            this.guildService = guildService;
            Connected += Client_ConnectedAsync;
            UserJoined += Client_UserJoinedAsync;
            UserLeft += Client_UserLeftAsync;
            JoinedGuild += Client_JoinedGuildAsync;
            LeftGuild += Client_LeftGuildAsync;
            GuildMemberUpdated += Client_GuildMemberUpdateAsync;
            Log += MonkeyClient_LogAsync;
        }

        private Task Client_ConnectedAsync()
        {
            logger.LogInformation("Connected");
            return Task.CompletedTask;
        }

        private async Task Client_UserJoinedAsync(SocketGuildUser user)
        {
            if (user.Guild == null)
            {
                return;
            }

            GuildConfig config = await guildService.GetOrCreateConfigAsync(user.Guild.Id).ConfigureAwait(false);
            string welcomeMessage = config?.WelcomeMessageText ?? string.Empty;
            if (config?.WelcomeMessageChannelId != null && !welcomeMessage.IsEmpty())
            {
                ITextChannel channel = user.Guild.GetTextChannel(config.WelcomeMessageChannelId) ?? user.Guild.DefaultChannel;                
                welcomeMessage = welcomeMessage.Replace("%server%", user.Guild.Name).Replace("%user%", user.Username);
                EmbedBuilder builder = new EmbedBuilder()
                    .WithColor(Color.DarkBlue)
                    .WithDescription(welcomeMessage)
                    .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                    .WithCurrentTimestamp();
                _ = await (channel?.SendMessageAsync(embed: builder.Build())).ConfigureAwait(false);
            }            
        }

        private async Task Client_UserLeftAsync(SocketGuildUser user)
        {
            if (user.Guild == null)
            {
                return;
            }

            GuildConfig config = await guildService.GetOrCreateConfigAsync(user.Guild.Id).ConfigureAwait(false);
            string goodbyeMessage = config?.GoodbyeMessageText ?? string.Empty;
            if (config?.GoodbyeMessageChannelId != null && !goodbyeMessage.IsEmpty())
            {
                ITextChannel channel = user.Guild.GetTextChannel(config.GoodbyeMessageChannelId) ?? user.Guild.DefaultChannel;
                goodbyeMessage = goodbyeMessage.Replace("%server%", user.Guild.Name).Replace("%user%", user.Username);
                EmbedBuilder builder = new EmbedBuilder()
                    .WithColor(Color.DarkBlue)
                    .WithDescription(goodbyeMessage)
                    .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                    .WithCurrentTimestamp();
                _ = await (channel?.SendMessageAsync(embed: builder.Build())).ConfigureAwait(false);
            }
        }

        private async Task Client_JoinedGuildAsync(SocketGuild guild)
        {
            logger.LogInformation($"Joined guild {guild.Name}");
            // Make sure to create the config;
            _ = await guildService.GetOrCreateConfigAsync(guild.Id).ConfigureAwait(false);
        }

        private async Task Client_LeftGuildAsync(SocketGuild guild)
        {
            logger.LogInformation($"Left guild {guild.Name}");
            await guildService.RemoveConfigAsync(guild.Id).ConfigureAwait(false);
        }

        private async Task Client_GuildMemberUpdateAsync(SocketGuildUser before, SocketGuildUser after)
        {
            if (after == null)
            {
                return;
            }

            GuildConfig config = await guildService.GetOrCreateConfigAsync(after.Guild.Id).ConfigureAwait(false);
            if (config == null || !config.StreamAnnouncementsEnabled || config.ConfirmedStreamerIds == null || !config.ConfirmedStreamerIds.Contains(after.Id))
            {
                // Streaming announcements has to be enabled for the guild and the streamer must first opt in to have it announced
                return;
            }

            if (before?.Activity?.Type != ActivityType.Streaming && after?.Activity?.Type == ActivityType.Streaming && after.Activity is StreamingGame stream)
            {
                ITextChannel channel = after.Guild.DefaultChannel;
                if (config?.DefaultChannelId != null)
                {
                    channel = after.Guild.GetTextChannel(config.GoodbyeMessageChannelId) ?? after.Guild.DefaultChannel;
                }
                _ = await (channel?.SendMessageAsync($"{after.Username} has started streaming. Watch it [here]({stream.Url}) ")).ConfigureAwait(false);
            }
        }

        private async Task MonkeyClient_LogAsync(LogMessage logMessage)
        {
            string msg = $"{logMessage.Source}: {logMessage.Message}";
            Exception ex = logMessage.Exception;
            if (logMessage.Severity <= LogSeverity.Warning && ConnectionState == ConnectionState.Connected && !ex.Message.Contains("WebSocket connection was closed", StringComparison.OrdinalIgnoreCase))
            {
                string adminMessage = $"{msg} {ex?.Message}";
                await NotifyAdminAsync(adminMessage).ConfigureAwait(false);
            }
            switch (logMessage.Severity)
            {
                case LogSeverity.Critical:
                    logger.LogCritical(ex, msg);
                    break;

                case LogSeverity.Error:
                    logger.LogError(ex, msg);
                    break;

                case LogSeverity.Warning:
                    logger.LogWarning(ex, msg);
                    break;

                case LogSeverity.Info:
                    logger.LogInformation(ex, msg);
                    break;

                case LogSeverity.Verbose:
                    logger.LogTrace(ex, msg);
                    break;

                case LogSeverity.Debug:
                    logger.LogDebug(ex, msg);
                    break;

                default:
                    break;
            }
            return;
        }

        public Task NotifyAdminAsync(string adminMessage)
        {
            SocketUser adminuser = GetUser(327885109560737793);
            return (adminuser?.SendMessageAsync(adminMessage));
        }
    }
}