using Discord;
using Discord.WebSocket;
using dokas.FluentStrings;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class MonkeyClient : DiscordSocketClient
    {
        private readonly ILogger<MonkeyClient> logger;
        private readonly DbService dbService;

        private static readonly DiscordSocketConfig discordConfig = new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Warning,
            MessageCacheSize = 1000
        };

        public MonkeyClient(ILogger<MonkeyClient> logger, DbService dbService) : base(discordConfig)
        {
            this.logger = logger;
            this.dbService = dbService;
            this.Log += MonkeyClient_LogAsync;
            this.UserJoined += Client_UserJoinedAsync;
            this.UserLeft += Client_UserLeftAsync;
            this.Connected += Client_ConnectedAsync;
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
            ITextChannel channel = arg.Guild.DefaultChannel;
            string welcomeMessage = string.Empty;
            using (var uow = dbService?.UnitOfWork)
            {
                var guildConfig = await uow.GuildConfigs.GetAsync(arg.Guild.Id).ConfigureAwait(false);
                welcomeMessage = guildConfig?.WelcomeMessageText;
                if (guildConfig?.WelcomeMessageChannelId != null)
                    channel = arg.Guild.GetTextChannel(guildConfig.WelcomeMessageChannelId) ?? arg.Guild.DefaultChannel;
            }
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
            ITextChannel channel = arg.Guild.DefaultChannel;
            string goodbyeMessage = string.Empty;
            using (var uow = dbService?.UnitOfWork)
            {
                var guildConfig = await uow.GuildConfigs.GetAsync(arg.Guild.Id).ConfigureAwait(false);
                goodbyeMessage = guildConfig?.GoodbyeMessageText;
                if (guildConfig?.GoodbyeMessageChannelId != null)
                    channel = arg.Guild.GetTextChannel(guildConfig.GoodbyeMessageChannelId) ?? arg.Guild.DefaultChannel;
            }
            if (!goodbyeMessage.IsEmpty())
            {
                goodbyeMessage = goodbyeMessage.Replace("%server%", arg.Guild.Name).Replace("%user%", arg.Username);
                await (channel?.SendMessageAsync(goodbyeMessage)).ConfigureAwait(false);
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