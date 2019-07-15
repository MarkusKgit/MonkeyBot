using Discord;
using Discord.WebSocket;
using dokas.FluentStrings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonkeyBot.Database;
using MonkeyBot.Models;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class MonkeyClient : DiscordSocketClient
    {
        private readonly ILogger<MonkeyClient> logger;
        private readonly MonkeyDBContext dbContext;

        private static readonly DiscordSocketConfig discordConfig = new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Warning,
            MessageCacheSize = 1000
        };

        public MonkeyClient(ILogger<MonkeyClient> logger, MonkeyDBContext dbContext) : base(discordConfig)
        {
            this.logger = logger;
            this.dbContext = dbContext;
            this.Connected += Client_ConnectedAsync;            
            this.UserJoined += Client_UserJoinedAsync;
            this.UserLeft += Client_UserLeftAsync;            
            this.JoinedGuild += Client_JoinedGuildAsync;
            this.LeftGuild += Client_LeftGuildAsync;
            this.Log += MonkeyClient_LogAsync;
        }
        
        private Task Client_ConnectedAsync()
        {
            logger.LogInformation("Connected");
            return Task.CompletedTask;
        }

        private async Task Client_UserJoinedAsync(SocketGuildUser arg)
        {
            if (arg.Guild == null)
                return;
            
            GuildConfig config = await dbContext.GuildConfigs.SingleOrDefaultAsync(c => c.GuildID == arg.Guild.Id).ConfigureAwait(false);
            string welcomeMessage = config?.WelcomeMessageText ?? string.Empty;
            ITextChannel channel = arg.Guild.DefaultChannel;
            if (config?.WelcomeMessageChannelId != null)
                channel = arg.Guild.GetTextChannel(config.WelcomeMessageChannelId) ?? arg.Guild.DefaultChannel;            
            if (!welcomeMessage.IsEmpty())
            {
                welcomeMessage = welcomeMessage.Replace("%server%", arg.Guild.Name).Replace("%user%", arg.Mention);
                await (channel?.SendMessageAsync(welcomeMessage)).ConfigureAwait(false);
            }
        }

        private async Task Client_UserLeftAsync(SocketGuildUser arg)
        {
            if (arg.Guild == null)
                return;

            GuildConfig config = await dbContext.GuildConfigs.SingleOrDefaultAsync(c => c.GuildID == arg.Guild.Id).ConfigureAwait(false);
            string goodbyeMessage = config?.GoodbyeMessageText ?? string.Empty;
            ITextChannel channel = arg.Guild.DefaultChannel;
            if (config?.GoodbyeMessageChannelId != null)
                channel = arg.Guild.GetTextChannel(config.GoodbyeMessageChannelId) ?? arg.Guild.DefaultChannel;

            if (!goodbyeMessage.IsEmpty())
            {
                goodbyeMessage = goodbyeMessage.Replace("%server%", arg.Guild.Name).Replace("%user%", arg.Username);
                await (channel?.SendMessageAsync(goodbyeMessage)).ConfigureAwait(false);
            }
        }

        private async Task Client_JoinedGuildAsync(SocketGuild arg)
        {
            logger.LogInformation($"Joined guild {arg.Name}");
            GuildConfig config = await dbContext.GuildConfigs.SingleOrDefaultAsync(c => c.GuildID == arg.Id).ConfigureAwait(false);
            if (config == null)
            {
                config = new GuildConfig();
                dbContext.GuildConfigs.Add(config);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        private async Task Client_LeftGuildAsync(SocketGuild arg)
        {
            logger.LogInformation($"Left guild {arg.Name}");
            GuildConfig config = await dbContext.GuildConfigs.SingleOrDefaultAsync(c => c.GuildID == arg.Id).ConfigureAwait(false);
            if (config != null)
            {
                dbContext.GuildConfigs.Remove(config);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        private async Task MonkeyClient_LogAsync(LogMessage logMessage)
        {
            var msg = $"{logMessage.Source}: {logMessage.Message}";
            var ex = logMessage.Exception;
            if (logMessage.Severity <= LogSeverity.Warning && ConnectionState == ConnectionState.Connected && !ex.Message.Contains("WebSocket connection was closed", StringComparison.OrdinalIgnoreCase))
            {
                var adminMessage = $"{msg} {ex?.Message}";
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

        public async Task NotifyAdminAsync(string adminMessage)
        {
            var adminuser = GetUser(327885109560737793);
            await (adminuser?.SendMessageAsync(adminMessage)).ConfigureAwait(false);
        }
    }
}