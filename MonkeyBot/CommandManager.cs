using Discord;
using Discord.Commands;
using Discord.WebSocket;
using dokas.FluentStrings;
using Microsoft.Extensions.DependencyInjection;
using MonkeyBot.Common;
using MonkeyBot.Services;
using MonkeyBot.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MonkeyBot
{
    /// <summary> Detect whether a message is a command, then execute it. </summary>
    public class CommandManager
    {
        private readonly IServiceProvider serviceProvider;
        private readonly DiscordSocketClient discordClient;
        private readonly DbService dbService;
        private readonly CommandService commandService;

        public CommandManager(IServiceProvider provider)
        {
            serviceProvider = provider;
            discordClient = provider.GetService<DiscordSocketClient>();
            dbService = provider.GetService<DbService>();
            commandService = provider.GetService<CommandService>();
        }

        public async Task StartAsync()
        {
            await commandService.AddModulesAsync(Assembly.GetEntryAssembly());    // Load all modules from the assembly.

            discordClient.MessageReceived += HandleCommandAsync;               // Register the messagereceived event to handle commands.
        }

        public Task<string> GetPrefixAsync(IGuild guild) => GetPrefixAsync(guild?.Id);

        public async Task<string> GetPrefixAsync(ulong? guildId)
        {
            if (guildId == null)
                return Configuration.DefaultPrefix;
            using (var uow = dbService.UnitOfWork)
            {
                var prefix = (await uow.GuildConfigs.GetAsync(guildId.Value))?.CommandPrefix;
                if (prefix != null)
                    return prefix;
                else
                    return Configuration.DefaultPrefix;
            }
        }

        private async Task HandleCommandAsync(SocketMessage socketMsg)
        {
            var msg = socketMsg as SocketUserMessage;
            if (msg == null)                                          // Check if the received message is from a user.
                return;

            var context = new SocketCommandContext(discordClient, msg);     // Create a new command context.

            var guild = (msg.Channel as SocketTextChannel)?.Guild;
            var prefix = await GetPrefixAsync(guild?.Id);

            int argPos = 0;                                           // Check if the message has either a string or mention prefix.
            if (msg.HasStringPrefix(prefix, ref argPos) ||
                msg.HasMentionPrefix(discordClient.CurrentUser, ref argPos))
            {                                                         // Try and execute a command with the given context.
                var result = await commandService.ExecuteAsync(context, argPos, serviceProvider);

                if (!result.IsSuccess)                                // If execution failed, reply with the error message.
                {
                    if (result.Error.HasValue && result.Error.Value == CommandError.UnknownCommand)
                    {
                        List<string> possibleCommands = new List<string>();
                        string commandText = msg.Content.Substring(argPos).ToLowerInvariant().Trim();
                        if (!commandText.IsEmpty())
                        {
                            foreach (var module in commandService.Modules)
                            {
                                foreach (var command in module.Commands)
                                {
                                    foreach (var alias in command.Aliases)
                                    {
                                        if (alias.ToLowerInvariant().Contains(commandText))
                                            possibleCommands.Add(alias);
                                    }
                                }
                            }
                            string message = $"Command *{msg.Content.Substring(argPos)}* was not found. Type {prefix}help to get a list of commands";
                            if (possibleCommands.Count == 1)
                                message = $"Did you mean *{possibleCommands.First()}* ? Type {prefix}help to get a list of commands";
                            else if (possibleCommands.Count > 1 && possibleCommands.Count < 5)
                                message = $"Did you mean one of the following commands:{Environment.NewLine}{string.Join(Environment.NewLine, possibleCommands)}{Environment.NewLine}Type {prefix}help to get a list of commands";
                            await context.Channel.SendMessageAsync(message);
                        }
                    }
                    else
                        await context.Channel.SendMessageAsync(result.ToString());
                }
            }
        }

        public async Task BuildDocumentationAsync()
        {
            string docuHTML = DocumentationBuilder.BuildDocumentation(commandService, DocumentationBuilder.OutputTypes.HTML);
            string fileHTML = Path.Combine(AppContext.BaseDirectory, "documentation.html");
            await Helpers.WriteTextAsync(fileHTML, docuHTML);

            string docuMD = DocumentationBuilder.BuildDocumentation(commandService, DocumentationBuilder.OutputTypes.MarkDown);
            string fileMD = Path.Combine(AppContext.BaseDirectory, "documentation.md");
            await Helpers.WriteTextAsync(fileMD, docuMD);
        }
    }
}