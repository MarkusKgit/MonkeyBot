using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonkeyBot.Common;
using MonkeyBot.Database;
using MonkeyBot.Services;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using System;
using System.Threading.Tasks;

namespace MonkeyBot
{
    public static class Initializer
    {
        public static async Task<IServiceProvider> InitializeAsync(ApplicationArguments args)
        {
            IServiceProvider services = ConfigureServices();

            ILoggerFactory loggerFactory = services.GetRequiredService<ILoggerFactory>();
            _ = loggerFactory.AddNLog(new NLogProviderOptions { CaptureMessageTemplates = true, CaptureMessageProperties = true });
            LogManager.Configuration = SetupNLogConfig();

            ILogger<MonkeyClient> logger = services.GetService<ILogger<MonkeyClient>>();

            DiscordSocketClient client = services.GetService<DiscordSocketClient>();
            await client.LoginAsync(TokenType.Bot, (await DiscordClientConfiguration.LoadAsync().ConfigureAwait(false)).Token).ConfigureAwait(false);
            await client.StartAsync().ConfigureAwait(false);

            CommandManager manager = services.GetService<CommandManager>();
            await manager.StartAsync().ConfigureAwait(false);

            MonkeyDBContext dbContext = services.GetRequiredService<MonkeyDBContext>();
            await DBInitializer.InitializeAsync(dbContext).ConfigureAwait(false);

            IAnnouncementService announcements = services.GetService<IAnnouncementService>();
            await announcements.InitializeAsync().ConfigureAwait(false);

            SteamGameServerService steamGameServerService = services.GetService<SteamGameServerService>();
            steamGameServerService.Initialize();

            MineCraftGameServerService minecraftGameServerService = services.GetService<MineCraftGameServerService>();
            minecraftGameServerService.Initialize();

            IGameSubscriptionService gameSubscriptionService = services.GetService<IGameSubscriptionService>();
            gameSubscriptionService.Initialize();

            IRoleButtonService roleButtonsService = services.GetService<IRoleButtonService>();
            roleButtonsService.Initialize();

            IFeedService feedService = services.GetService<IFeedService>();
            feedService.Start();

            IBattlefieldNewsService battlefieldNewsService = services.GetService<IBattlefieldNewsService>();
            battlefieldNewsService.Start();

            if (args != null && args.BuildDocumentation)
            {
                await manager.BuildDocumentationAsync().ConfigureAwait(false); // Write the documentation
                logger.LogInformation("Documentation built");
            }

            return services;
        }

#pragma warning disable CA2000 // Dispose objects before losing scope
        private static LoggingConfiguration SetupNLogConfig()
        {
            var logConfig = new LoggingConfiguration();

            var coloredConsoleTarget = new ColoredConsoleTarget("logconsole")
            {
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} ${logger:shortName=True} | ${message} ${exception}"
            };

            logConfig.AddTarget(coloredConsoleTarget);

            var debugTarget = new DebuggerTarget
            {
                Name = "debugConsole",
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} ${logger:shortName=True} | ${message} ${exception}"
            };
            logConfig.AddTarget(debugTarget);

            var fileTarget = new FileTarget
            {
                Name = "logFile",
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} ${logger:shortName=True} | ${message} ${exception}",
                FileName = "${basedir}/Logs/${level}.log",
                ArchiveFileName = "${basedir}/Logs/Archive/${level}.{##}.log",
                ArchiveNumbering = ArchiveNumberingMode.Sequence,
                ArchiveAboveSize = 1_000_000,
                ConcurrentWrites = false,
                MaxArchiveFiles = 20
            };
            logConfig.AddTarget(fileTarget);

            logConfig.AddRule(NLog.LogLevel.Warn, NLog.LogLevel.Fatal, coloredConsoleTarget);
            logConfig.AddRuleForAllLevels(debugTarget);
            logConfig.AddRuleForAllLevels(fileTarget);

            return logConfig;
        }
#pragma warning restore CA2000 // Dispose objects before losing scope

        private static IServiceProvider ConfigureServices()
        {
            IServiceCollection services = new ServiceCollection()
                .AddSingleton<ILoggerFactory, LoggerFactory>()
                .AddSingleton(typeof(ILogger<>), typeof(Logger<>))
                .AddLogging((builder) => builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace))
                .AddDbContext<MonkeyDBContext>(ServiceLifetime.Transient)
                .AddSingleton<DiscordSocketClient, MonkeyClient>()
                .AddSingleton<InteractiveService>()
                .AddSingleton<CommandService, MonkeyCommandService>()
                .AddSingleton<CommandManager>()
                .AddSingleton<ISchedulingService, SchedulingService>()
                .AddSingleton<IAnnouncementService, AnnouncementService>()
                .AddSingleton<ITriviaService, OTDBTriviaService>()
                .AddSingleton<IFeedService, FeedService>()
                .AddSingleton<IBattlefieldNewsService, BattlefieldNewsService>()
                .AddSingleton<SteamGameServerService>()
                .AddSingleton<MineCraftGameServerService>()
                .AddSingleton<IGameSubscriptionService, GameSubscriptionService>()
                .AddSingleton<IRoleButtonService, RoleButtonService>()
                .AddSingleton<IChuckService, ChuckService>()
                .AddSingleton<IPictureUploadService, CloudinaryPictureUploadService>()
                .AddSingleton<IDogService, DogService>()
                .AddSingleton<IXkcdService, XkcdService>();

            IServiceProvider provider = new DefaultServiceProviderFactory().CreateServiceProvider(services);
            return provider;
        }
    }
}