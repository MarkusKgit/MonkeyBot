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
            var discordClient = await StartDiscordClient();
            services.AddSingleton(discordClient);
            var commandService = BuildCommandService();
            services.AddSingleton(commandService);
            services.AddSingleton<CommandManager>();
            services.AddDbContext<TriviaScoresDB>(ServiceLifetime.Transient);
            services.AddSingleton<IAnnouncementService>(new AnnouncementService(discordClient));
            services.AddSingleton(typeof(ITriviaService), typeof(OTDBTriviaService));
            
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

        private async Task<DiscordSocketClient> StartDiscordClient()
        {
            DiscordSocketConfig discordConfig = new DiscordSocketConfig(); //Create a new config for the Discord Client
            discordConfig.LogLevel = LogSeverity.Verbose;
            discordConfig.MessageCacheSize = 1000;
            var discordClient = new DiscordSocketClient(discordConfig);    // Create a new instance of DiscordSocketClient with the specified config.

            discordClient.Log += (l) => Console.Out.WriteLineAsync(l.ToString()); // Log to console for now
            discordClient.UserJoined += Client_UserJoined;
            discordClient.Connected += Client_Connected;

            await discordClient.LoginAsync(TokenType.Bot, (await Configuration.LoadAsync()).TestingToken);
            await discordClient.StartAsync();
            return discordClient;
        }
                
        private async Task Client_Connected()
        {
            await Console.Out.WriteLineAsync("Connected");
        }

        private async Task Client_UserJoined(SocketGuildUser arg)
        {
            var channel = arg.Guild.DefaultChannel;
            await channel?.SendMessageAsync("Hello there " + arg.Mention + "! Welcome to Monkey-Gamers. Read our welcome page for rules and info or type !rules for a list of rules and !help for a list of commands you can use with our bot. If you have any issues feel free to contact our Admins or Leaders."); //Welcomes the new user
        }
    }
}
