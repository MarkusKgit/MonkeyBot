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
            var services = ConfigureServices();

            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            loggerFactory.AddNLog(new NLogProviderOptions { CaptureMessageTemplates = true, CaptureMessageProperties = true });
            LogManager.Configuration = SetupNLogConfig();

            var logger = services.GetService<ILogger<MonkeyClient>>();

            var client = services.GetService<DiscordSocketClient>();
            await client.LoginAsync(TokenType.Bot, (await DiscordClientConfiguration.LoadAsync().ConfigureAwait(false)).Token).ConfigureAwait(false);
            await client.StartAsync().ConfigureAwait(false);

            var manager = services.GetService<CommandManager>();
            await manager.StartAsync().ConfigureAwait(false);

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
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            services.AddLogging((builder) => builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace));
            services.AddDbContext<MonkeyDBContext>(ServiceLifetime.Transient);
            services.AddSingleton<DiscordSocketClient, MonkeyClient>();
            services.AddSingleton<InteractiveService>();
            services.AddSingleton<CommandService, MonkeyCommandService>();
            services.AddSingleton<CommandManager>();
            services.AddSingleton<ISchedulingService, SchedulingService>();
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

            var provider = new DefaultServiceProviderFactory().CreateServiceProvider(services);
            return provider;
        }
    }
}