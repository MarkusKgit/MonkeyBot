using AutoMapper;
using AutoMapper.Configuration;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FluentScheduler;
using Microsoft.Extensions.DependencyInjection;
using MonkeyBot.Common;
using MonkeyBot.Database.Entities;
using MonkeyBot.Services;
using MonkeyBot.Services.Common.Trivia;
using MonkeyBot.Services.Implementations;
using System;
using System.Threading.Tasks;

namespace MonkeyBot
{
    public static class Initializer
    {
        public static async Task InitializeAsync()
        {
            InitializeMapper();

            var services = await ConfigureServicesAsync();

            var manager = services.GetService<CommandManager>();
            await manager.StartAsync();

            var eventHandler = services.GetService<EventHandlerService>();
            eventHandler.Start();

            var registry = services.GetService<Registry>();
            JobManager.Initialize(registry);

            var announcements = services.GetService<IAnnouncementService>();
            await announcements.InitializeAsync();

            var backgroundTasks = services.GetService<IBackgroundService>();
            backgroundTasks.Start();

            await manager.BuildDocumentationAsync(); // Write the documentation
        }

        private static void InitializeMapper()
        {
            var cfg = new MapperConfigurationExpression();
            cfg.CreateMap<GuildConfigEntity, GuildConfig>();
            cfg.CreateMap<TriviaScoreEntity, TriviaScore>();
            Mapper.Initialize(cfg);
        }

        private static async Task<IServiceProvider> ConfigureServicesAsync()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new DbService());
            var discordClient = await StartDiscordClientAsync();
            services.AddSingleton(discordClient);
            var commandService = BuildCommandService();
            services.AddSingleton(commandService);
            services.AddSingleton<CommandManager>();
            services.AddSingleton(typeof(IAnnouncementService), typeof(AnnouncementService));
            services.AddSingleton(typeof(ITriviaService), typeof(OTDBTriviaService));
            services.AddSingleton(typeof(IPollService), typeof(PollService));
            services.AddSingleton<EventHandlerService>();
            services.AddSingleton(typeof(IBackgroundService), typeof(BackgroundService));
            services.AddSingleton(new Registry());

            var provider = new DefaultServiceProviderFactory().CreateServiceProvider(services);
            return provider;
        }

        private static CommandService BuildCommandService()
        {
            CommandServiceConfig commandConfig = new CommandServiceConfig();
            commandConfig.CaseSensitiveCommands = false;
            commandConfig.DefaultRunMode = RunMode.Async;
            commandConfig.LogLevel = LogSeverity.Warning;
            commandConfig.ThrowOnError = false;
            var commandService = new CommandService(commandConfig); // Create a new instance of the commandservice.
            commandService.Log += (l) => Console.Out.WriteLineAsync(l.ToString()); // Log to console for now
            return commandService;
        }

        private static async Task<DiscordSocketClient> StartDiscordClientAsync()
        {
            DiscordSocketConfig discordConfig = new DiscordSocketConfig(); //Create a new config for the Discord Client
            discordConfig.LogLevel = LogSeverity.Warning;
            discordConfig.MessageCacheSize = 1000;
            var discordClient = new DiscordSocketClient(discordConfig);    // Create a new instance of DiscordSocketClient with the specified config.

            discordClient.Log += (l) => Console.Out.WriteLineAsync(l.ToString()); // Log to console for now

            await discordClient.LoginAsync(TokenType.Bot, (await Configuration.LoadAsync()).ProductiveToken);
            await discordClient.StartAsync();
            return discordClient;
        }
    }
}