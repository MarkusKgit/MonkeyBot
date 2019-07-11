using AutoMapper;
using AutoMapper.Configuration;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using dokas.FluentStrings;
using FluentScheduler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonkeyBot.Common;
using MonkeyBot.Database.Entities;
using MonkeyBot.Services;
using MonkeyBot.Services.Common;
using MonkeyBot.Services.Common.Announcements;
using MonkeyBot.Services.Common.Feeds;
using MonkeyBot.Services.Common.GameSubscription;
using MonkeyBot.Services.Common.RoleButtons;
using MonkeyBot.Services.Common.Trivia;
using MonkeyBot.Services.Implementations;
using NLog.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace MonkeyBot
{
    public static class Initializer
    {
        public static async Task<IServiceProvider> InitializeAsync(ApplicationArguments args)
        {
            InitializeMapper();

            var services = ConfigureServices();

            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            loggerFactory.AddNLog(new NLogProviderOptions { CaptureMessageTemplates = true, CaptureMessageProperties = true });
            NLog.LogManager.Configuration = SetupNLogConfig();

            var logger = services.GetService<ILogger<MonkeyClient>>();

            var client = services.GetService<DiscordSocketClient>();
            await client.LoginAsync(TokenType.Bot, (await DiscordClientConfiguration.LoadAsync()).Token);
            await client.StartAsync();

            var manager = services.GetService<CommandManager>();
            await manager.StartAsync();

            var registry = services.GetService<Registry>();
            JobManager.Initialize(registry);

            var announcements = services.GetService<IAnnouncementService>();
            await announcements.InitializeAsync();

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
                await manager.BuildDocumentationAsync(); // Write the documentation
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
            var infoLoggingRule = new NLog.Config.LoggingRule("*", NLog.LogLevel.Info, coloredConsoleTarget);
            logConfig.LoggingRules.Add(infoLoggingRule);
            return logConfig;
        }

        private static void InitializeMapper()
        {
            var cfg = new MapperConfigurationExpression();
            cfg.CreateMap<GuildConfigEntity, GuildConfig>();
            cfg.CreateMap<FeedEntity, FeedDTO>();
            cfg.CreateMap<TriviaScoreEntity, TriviaScore>();
            cfg.CreateMap<GameServerEntity, DiscordGameServerInfo>();
            cfg.CreateMap<GameSubscriptionEntity, GameSubscription>();
            cfg.CreateMap<RoleButtonLinkEntity, RoleButtonLink>();
            cfg.CreateMap<AnnouncementEntity, Announcement>().ConstructUsing(x => GetAnnouncement(x));

            Mapper.Initialize(cfg);
        }

        private static Announcement GetAnnouncement(AnnouncementEntity item)
        {
            if (item.Type == AnnouncementType.Recurring && !item.CronExpression.IsEmpty())
                return new RecurringAnnouncement(item.Name, item.CronExpression, item.Message, item.GuildId, item.ChannelId);
            if (item.Type == AnnouncementType.Single && item.ExecutionTime.HasValue)
                return new SingleAnnouncement(item.Name, item.ExecutionTime.Value, item.Message, item.GuildId, item.ChannelId);
            return null;
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            services.AddLogging((builder) => builder.SetMinimumLevel(LogLevel.Trace));
            services.AddSingleton(new DbService());
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
            services.AddSingleton(new Registry());

            var provider = new DefaultServiceProviderFactory().CreateServiceProvider(services);
            return provider;
        }
    }
}