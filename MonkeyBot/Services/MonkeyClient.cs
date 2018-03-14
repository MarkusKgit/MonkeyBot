using Discord;
using Discord.WebSocket;
using dokas.FluentStrings;
using Microsoft.Extensions.Logging;
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

        public MonkeyClient(ILogger<MonkeyClient> logger, DbService db) : base(discordConfig)
        {
            this.logger = logger;
            this.dbService = db;
            this.Log += MonkeyClient_LogAsync;
            this.UserJoined += Client_UserJoinedAsync;
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
            var channel = arg.Guild.DefaultChannel;
            string welcomeMessage = string.Empty;
            using (var uow = dbService?.UnitOfWork)
            {
                welcomeMessage = (await uow.GuildConfigs.GetAsync(arg.Guild.Id))?.WelcomeMessageText;
            }
            if (!welcomeMessage.IsEmpty())
            {
                welcomeMessage = welcomeMessage.Replace("%server%", arg.Guild.Name);
                welcomeMessage = welcomeMessage.Replace("%user%", arg.Mention);
                await channel?.SendMessageAsync(welcomeMessage);
            }
        }

        private async Task MonkeyClient_LogAsync(LogMessage logMessage)
        {
            var msg = $"{logMessage.Source}: {logMessage.Message}";
            var ex = logMessage.Exception;
            if (logMessage.Severity <= LogSeverity.Warning && ConnectionState == ConnectionState.Connected)
            {
                var adminuser = GetUser(327885109560737793);
                await adminuser?.SendMessageAsync($"{msg} {ex?.Message}");
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
    }
}