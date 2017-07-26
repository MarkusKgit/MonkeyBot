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
    public class CommandHandler
    {
        private DiscordSocketClient client;
        private CommandService commandService;
        private IServiceProvider services;
        private ITriviaService triviaService;

        public CommandService CommandService
        {
            get { return commandService; }
        }

        public async Task InstallAsync(DiscordSocketClient client)
        {
            this.client = client;                                                 // Save an instance of the discord client.
            CommandServiceConfig commandConfig = new CommandServiceConfig();
            commandConfig.CaseSensitiveCommands = false;
            commandConfig.DefaultRunMode = RunMode.Async;
            commandConfig.LogLevel = LogSeverity.Error;
            commandConfig.ThrowOnError = true;
            commandService = new CommandService(commandConfig);                    // Create a new instance of the commandservice.

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IAnnouncementService>(new AnnouncementService(client));
            serviceCollection.AddSingleton<ITriviaService>(new OTDBTriviaService(client));
            services = serviceCollection.BuildServiceProvider();

            await commandService.AddModulesAsync(Assembly.GetEntryAssembly());    // Load all modules from the assembly.

            this.client.MessageReceived += HandleCommandAsync;               // Register the messagereceived event to handle commands.
        }

        private async Task HandleCommandAsync(SocketMessage socketMsg)
        {
            var msg = socketMsg as SocketUserMessage;
            if (msg == null)                                          // Check if the received message is from a user.
                return;

            var context = new SocketCommandContext(client, msg);     // Create a new command context.

            int argPos = 0;                                           // Check if the message has either a string or mention prefix.
            if (msg.HasStringPrefix((await Configuration.LoadAsync()).Prefix, ref argPos) ||
                msg.HasMentionPrefix(client.CurrentUser, ref argPos))
            {                                                         // Try and execute a command with the given context.
                var result = await commandService.ExecuteAsync(context, argPos, services);

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