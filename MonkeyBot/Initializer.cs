using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using FluentScheduler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonkeyBot.Common;
using MonkeyBot.Database;
using MonkeyBot.Services;
using NLog.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace MonkeyBot
{
    public static class Initializer
    {
        public static async Task<IServiceProvider> InitializeAsync(ApplicationArguments args)
        {
            var services = ConfigureServices();

            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            loggerFactory.AddNLog(new NLogProviderOptions { CaptureMessageTemplates = true, CaptureMessageProperties = true });
            NLog.LogManager.Configuration = SetupNLogConfig();

            var logger = services.GetService<ILogger<MonkeyClient>>();

            var client = services.GetService<DiscordSocketClient>();
            await client.LoginAsync(TokenType.Bot, (await DiscordClientConfiguration.LoadAsync().ConfigureAwait(false)).Token).ConfigureAwait(false);
            await client.StartAsync().ConfigureAwait(false);

            var manager = services.GetService<CommandManager>();
            await manager.StartAsync().ConfigureAwait(false);

            var registry = services.GetService<Registry>();
            JobManager.Initialize(registry);

            var dbContext = services.GetRequiredService<MonkeyDBContext>();
            await DBInitializer.InitializeAsync(dbContext).ConfigureAwait(false);

            var announcements = services.GetService<IAnnouncementService>();
            await announcements.InitializeAsync().ConfigureAwait(false);

            var steamGameServerService = services.GetService<SteamGameServerService>();
            steamGameServerService.Initialize();

            var minecraftGameServerService = services.GetService<MineCraftGameServerService>();
            minecraftGameServerService.Initialize();

            var gameSubscriptionService = services.GetService<IGameSubscriptionService>();
            gameSubscriptionService.Initialize();

            var roleButtonsService = services.GetService<IRoleButtonService>();
            roleButtonsService.Initialize();

            var feedService = services.GetService<IFeedService>();
            feedService.Start();

            if (args != null && args.BuildDocumentation)
            {
                await manager.BuildDocumentationAsync().ConfigureAwait(false); // Write the documentation
                logger.LogInformation("Documentation built");
            }

            return services;
        }

        private static NLog.Config.LoggingConfiguration SetupNLogConfig()
        {
            var logConfig = new NLog.Config.LoggingConfiguration();
            var coloredConsoleTarget = new NLog.Targets.ColoredConsoleTarget
            {
                Name = "logconsole",
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} ${logger:shortName=True} | ${message} ${exception}"
            };
            var productionLoggingRule = new NLog.Config.LoggingRule("*", NLog.LogLevel.Warn, coloredConsoleTarget);
            logConfig.LoggingRules.Add(productionLoggingRule);

            var debugTarget = new NLog.Targets.DebuggerTarget
            {
                Name = "debugConsole",
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} ${logger:shortName=True} | ${message} ${exception}"
            };
            var debugLoggingRule = new NLog.Config.LoggingRule("*", NLog.LogLevel.Info, debugTarget);
            logConfig.LoggingRules.Add(debugLoggingRule);

            var fileTarget = new NLog.Targets.FileTarget
            {
                Name = "logFile",
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} ${logger:shortName=True} | ${message} ${exception}",
                FileName = "${basedir}\\Logs\\${level}.log",
                ArchiveFileName = "${basedir}\\Logs\\Archive\\${level}.{##}.log",
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Sequence,
                ArchiveAboveSize = 1_000_000,
                ConcurrentWrites = false,
                MaxArchiveFiles = 20
            };
            var fileLoggingRule = new NLog.Config.LoggingRule("*", NLog.LogLevel.Info, fileTarget);
            logConfig.LoggingRules.Add(fileLoggingRule);

            return logConfig;
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            services.AddLogging((builder) => builder.SetMinimumLevel(LogLevel.Trace));
            services.AddDbContext<MonkeyDBContext>(ServiceLifetime.Transient);
            services.AddSingleton<DiscordSocketClient, MonkeyClient>();
            services.AddSingleton<InteractiveService>();
            services.AddSingleton<CommandService, MonkeyCommandService>();
            services.AddSingleton<CommandManager>();
            services.AddSingleton<IAnnouncementService, AnnouncementService>();
            services.AddSingleton<ITriviaService, OTDBTriviaService>();
            services.AddSingleton<IFeedService, FeedService>();
            services.AddSingleton<SteamGameServerService>();
            services.AddSingleton<MineCraftGameServerService>();
            services.AddSingleton<IGameSubscriptionService, GameSubscriptionService>();
            services.AddSingleton<IRoleButtonService, RoleButtonService>();
            services.AddSingleton<IChuckService, ChuckService>();
            services.AddSingleton<IPictureUploadService, CloudinaryPictureUploadService>();
            services.AddSingleton<IDogService, DogService>();
            services.AddSingleton<IXkcdService, XkcdService>();
            services.AddSingleton(new Registry());

            var provider = new DefaultServiceProviderFactory().CreateServiceProvider(services);
            return provider;
        }
    }
}