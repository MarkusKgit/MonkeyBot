using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MonkeyBot.Common;
using MonkeyBot.Services;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace MonkeyBot
{
    /// <summary> Detect whether a message is a command, then execute it. </summary>
    public class CommandManager
    {
        private DiscordSocketClient discordClient;
        private CommandService commandService;
        private IServiceProvider serviceProvider;

        public CommandService CommandService
        {
            get { return commandService; }
        }

        public CommandManager(IServiceProvider provider)
        {
            serviceProvider = provider;
            discordClient = provider.GetService<DiscordSocketClient>();
            commandService = provider.GetService<CommandService>();
        }

        public async Task StartAsync()
        {   
            

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IAnnouncementService>(new AnnouncementService(client));
            serviceCollection.AddSingleton<ITriviaService>(new OTDBTriviaService(client));
            serviceProvider = serviceCollection.BuildServiceProvider();

            await commandService.AddModulesAsync(Assembly.GetEntryAssembly());    // Load all modules from the assembly.

            discordClient.MessageReceived += HandleCommandAsync;               // Register the messagereceived event to handle commands.
        }

        private async Task HandleCommandAsync(SocketMessage socketMsg)
        {
            var msg = socketMsg as SocketUserMessage;
            if (msg == null)                                          // Check if the received message is from a user.
                return;

            var context = new SocketCommandContext(discordClient, msg);     // Create a new command context.

            int argPos = 0;                                           // Check if the message has either a string or mention prefix.
            if (msg.HasStringPrefix((await Configuration.LoadAsync()).Prefix, ref argPos) ||
                msg.HasMentionPrefix(discordClient.CurrentUser, ref argPos))
            {                                                         // Try and execute a command with the given context.
                var result = await commandService.ExecuteAsync(context, argPos, serviceProvider);

                if (!result.IsSuccess)                                // If execution failed, reply with the error message.
                {
                    if (result.Error.HasValue && result.Error.Value == CommandError.UnknownCommand)
                        await context.Channel.SendMessageAsync($"Command *{msg.Content.Substring(argPos)}* was not found. Type !help to get a list of commands");
                    else
                        await context.Channel.SendMessageAsync(result.ToString());
                }
            }
        }
    }
}