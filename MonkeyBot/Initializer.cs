using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MonkeyBot.Common;
using MonkeyBot.Databases;
using MonkeyBot.Services;
using System;
using System.Threading.Tasks;

namespace MonkeyBot
{
    public class Initializer
    {
        public async Task<IServiceProvider> ConfigureServices()
        {
            var services = new ServiceCollection();            
            var discordClient = await StartDiscord();
            services.AddSingleton(discordClient);
            var commandService = BuildCommandService();
            services.AddSingleton(commandService);
            services.AddSingleton<CommandManager>();
            services.AddDbContext<TriviaScoresDB>(ServiceLifetime.Transient);
            services.AddSingleton<IAnnouncementService>(new AnnouncementService(discordClient));
            services.AddSingleton<ITriviaService>(new OTDBTriviaService(discordClient));
            
            var provider = new DefaultServiceProviderFactory().CreateServiceProvider(services);
            return provider;
        }

        private CommandService BuildCommandService()
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

        private async Task<DiscordSocketClient> StartDiscord()
        {
            DiscordSocketConfig discordConfig = new DiscordSocketConfig(); //Create a new config for the Discord Client
            discordConfig.LogLevel = LogSeverity.Verbose;
            discordConfig.MessageCacheSize = 1000;
            var discordClient = new DiscordSocketClient(discordConfig);    // Create a new instance of DiscordSocketClient with the specified config.

            discordClient.Log += (l) => Console.Out.WriteLineAsync(l.ToString()); // Log to console for now

            await discordClient.LoginAsync(TokenType.Bot, (await Configuration.LoadAsync()).ProductiveToken);
            await discordClient.StartAsync();
            return discordClient;
        }        
    }
}
