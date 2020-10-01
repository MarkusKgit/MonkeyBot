using DSharpPlus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonkeyBot.Database;
using MonkeyBot.Services;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using System;
using System.Threading.Tasks;

namespace MonkeyBot
{
    public static class Initializer
    {
        public static async Task InitializeServicesAsync(IServiceProvider services)
        {
            MonkeyDBContext dbContext = services.GetRequiredService<MonkeyDBContext>();
            await DBInitializer.InitializeAsync(dbContext).ConfigureAwait(false);

            IAnnouncementService announcements = services.GetService<IAnnouncementService>();
            await announcements.InitializeAsync().ConfigureAwait(false);

            SteamGameServerService steamGameServerService = services.GetService<SteamGameServerService>();
            steamGameServerService.Initialize();

            MineCraftGameServerService minecraftGameServerService = services.GetService<MineCraftGameServerService>();
            minecraftGameServerService.Initialize();

            IRoleButtonService roleButtonsService = services.GetService<IRoleButtonService>();
            roleButtonsService.Initialize();

            IFeedService feedService = services.GetService<IFeedService>();
            feedService.Start();

            IBattlefieldNewsService battlefieldNewsService = services.GetService<IBattlefieldNewsService>();
            battlefieldNewsService.Start();

            IPollService pollService = services.GetService<IPollService>();
            await pollService.InitializeAsync().ConfigureAwait(false);

            return;
        }

#pragma warning disable CA2000 // Dispose objects before losing scope
        private static LoggingConfiguration SetupNLogConfig()
        {
            var logConfig = new LoggingConfiguration();

            var coloredConsoleTarget = new ColoredConsoleTarget("logconsole")
            {
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} ${logger:shortName=True} | ${message} ${exception:format=ToString,Data:exceptionDataSeparator=\r\n}"
            };

            logConfig.AddTarget(coloredConsoleTarget);

            var debugTarget = new DebuggerTarget
            {
                Name = "debugConsole",
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} ${logger:shortName=True} | ${message} ${exception:format=ToString,Data:exceptionDataSeparator=\r\n}"
            };
            logConfig.AddTarget(debugTarget);

            var fileTarget = new FileTarget
            {
                Name = "logFile",
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} ${logger:shortName=True} | ${message} ${exception:format=ToString,Data:exceptionDataSeparator=\r\n}",
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

        public static IServiceProvider ConfigureServices(DiscordClient discordClient)
        {
            IServiceCollection services = new ServiceCollection()
                .AddLogging(loggingBuilder => 
                    {
                        _ = loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                        _ = loggingBuilder.AddNLog(SetupNLogConfig());
                    })
                .AddHttpClient()
                .AddDbContext<MonkeyDBContext>(ServiceLifetime.Transient)
                .AddSingleton(discordClient)
                .AddSingleton<IGuildService, GuildService>()
                .AddSingleton<ISchedulingService, SchedulingService>()
                .AddSingleton<IAnnouncementService, AnnouncementService>()                
                .AddSingleton<IFeedService, FeedService>()
                .AddSingleton<IBattlefieldNewsService, BattlefieldNewsService>()
                .AddSingleton<SteamGameServerService>()
                .AddSingleton<MineCraftGameServerService>()
                .AddSingleton<IRoleButtonService, RoleButtonService>()
                .AddSingleton<IChuckService, ChuckService>()
                .AddSingleton<ICatService, CatService>()
                .AddSingleton<IDogService, DogService>()
                .AddSingleton<IXkcdService, XkcdService>()
                .AddSingleton<IPictureSearchService, GoogleImageSearchService>()
                .AddSingleton<ITriviaService, TriviaService>()
                .AddSingleton<IPollService, PollService>();

            IServiceProvider provider = new DefaultServiceProviderFactory().CreateServiceProvider(services);
            return provider;
        }
    }
}