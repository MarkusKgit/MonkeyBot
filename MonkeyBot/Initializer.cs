using Discord;
using Discord.Commands;
using Discord.WebSocket;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using MonkeyBot.Services;
using System;
using System.Threading.Tasks;
using AutoMapper.Configuration;
using MonkeyBot.Database.Entities;
using MonkeyBot.Common;

namespace MonkeyBot
{
    public static class Initializer
    {
        public static Task<IServiceProvider> InitializeAsync()
        {
            InitializeMapper();
            return ConfigureServicesAsync();
        }

        private static void InitializeMapper()
        {
            var cfg = new MapperConfigurationExpression();
            cfg.CreateMap<GuildConfigEntity, Common.GuildConfig>();
            cfg.CreateMap<TriviaScoreEntity, Services.Common.Trivia.TriviaScore>();
            Mapper.Initialize(cfg);
        }

        private static async Task<IServiceProvider> ConfigureServicesAsync()
        {
            var services = new ServiceCollection();
            services.AddSingleton<DbService>();
            var discordClient = await StartDiscordClientAsync();
            services.AddSingleton(discordClient);
            var commandService = BuildCommandService();
            services.AddSingleton(commandService);
            services.AddSingleton<CommandManager>();
            services.AddSingleton(typeof(IAnnouncementService), typeof(AnnouncementService));
            services.AddSingleton(typeof(ITriviaService), typeof(OTDBTriviaService));
            services.AddSingleton<EventHandlerService>();

            var provider = new DefaultServiceProviderFactory().CreateServiceProvider(services);
            return provider;
        }

        private static CommandService BuildCommandService()
        {
            CommandServiceConfig commandConfig = new CommandServiceConfig();
            commandConfig.CaseSensitiveCommands = false;
            commandConfig.DefaultRunMode = RunMode.Async;
            commandConfig.LogLevel = LogSeverity.Verbose;
            commandConfig.ThrowOnError = false;
            var commandService = new CommandService(commandConfig); // Create a new instance of the commandservice.
            commandService.Log += (l) => Console.Out.WriteLineAsync(l.ToString()); // Log to console for now
            return commandService;
        }

        private static async Task<DiscordSocketClient> StartDiscordClientAsync()
        {
            DiscordSocketConfig discordConfig = new DiscordSocketConfig(); //Create a new config for the Discord Client
            discordConfig.LogLevel = LogSeverity.Verbose;
            discordConfig.MessageCacheSize = 1000;
            var discordClient = new DiscordSocketClient(discordConfig);    // Create a new instance of DiscordSocketClient with the specified config.

            discordClient.Log += (l) => Console.Out.WriteLineAsync(l.ToString()); // Log to console for now

            await discordClient.LoginAsync(TokenType.Bot, (await Configuration.LoadAsync()).TestingToken);
            await discordClient.StartAsync();
            return discordClient;
        }
    }
}