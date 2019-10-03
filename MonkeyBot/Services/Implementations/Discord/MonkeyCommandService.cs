using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class MonkeyCommandService : CommandService
    {
        private readonly ILogger<MonkeyCommandService> logger;

        private static readonly CommandServiceConfig cfg = new CommandServiceConfig
        {
            CaseSensitiveCommands = false,
            DefaultRunMode = RunMode.Async,
            LogLevel = LogSeverity.Warning,
            ThrowOnError = false
        };

        public MonkeyCommandService(ILogger<MonkeyCommandService> logger) : base(cfg)
        {
            this.logger = logger;
            Log += MonkeyCommandService_LogAsync;
        }

        private Task MonkeyCommandService_LogAsync(LogMessage logMessage)
        {
            string msg = $"{logMessage.Source}: {logMessage.Message}";
            Exception ex = logMessage.Exception;
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
            return Task.CompletedTask;
        }
    }
}