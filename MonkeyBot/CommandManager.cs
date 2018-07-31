using Discord;
using Discord.Commands;
using Discord.WebSocket;
using dokas.FluentStrings;
using Microsoft.Extensions.DependencyInjection;
using MonkeyBot.Common;
using MonkeyBot.Services;
using MonkeyBot.Utilities;
using System;
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

        /// <summary>
        /// Create a new CommandManager instance with DI. Use <see cref="StartAsync"/> afterwards to actually start the CommandManager/>
        /// </summary>
        public CommandManager(IServiceProvider provider)
        {
            serviceProvider = provider;
            discordClient = provider.GetService<DiscordSocketClient>();
            dbService = provider.GetService<DbService>();
            commandService = provider.GetService<CommandService>();
        }

        public async Task StartAsync()
        {
            await commandService.AddModulesAsync(Assembly.GetEntryAssembly());

            discordClient.MessageReceived += HandleCommandAsync;
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
            if (!(socketMsg is SocketUserMessage msg))
                return;
            var context = new SocketCommandContext(discordClient, msg);
            var guild = (msg.Channel as SocketTextChannel)?.Guild;
            var prefix = await GetPrefixAsync(guild?.Id);
            int argPos = 0;

            if (msg.HasStringPrefix(prefix, ref argPos))
            {
                string commandText = msg.Content.Substring(argPos).ToLowerInvariant().Trim();
                if (!commandText.IsEmpty())
                {
                    var result = await commandService.ExecuteAsync(context, argPos, serviceProvider);

                    if (!result.IsSuccess)
                    {
                        if (result.Error.HasValue)
                        {
                            var error = result.Error.Value;
                            var errorMessage = GetCommandErrorMessage(error, prefix, commandText);
                            await context.Channel.SendMessageAsync(errorMessage);
                            if (error == CommandError.Exception || error == CommandError.ParseFailed || error == CommandError.Unsuccessful)
                            {
                                if (discordClient is MonkeyClient monkeyClient)
                                {
                                    await monkeyClient.NotifyAdminAsync(errorMessage);
                                }
                            }
                        }
                        else
                        {
                            await context.Channel.SendMessageAsync(result.ToString());
                        }
                    }
                }
            }
        }

        private string GetCommandErrorMessage(CommandError error, string prefix, string commandText)
        {
            switch (error)
            {
                case CommandError.UnknownCommand:
                    {
                        var possibleCommands =
                        commandService
                            .Modules
                            .SelectMany(module => module.Commands)
                            .SelectMany(command => command.Aliases)
                            .Where(alias => alias.ToLowerInvariant().Contains(commandText))
                            .ToList();

                        string message = "";
                        if (possibleCommands == null || possibleCommands.Count < 1)
                        {
                            message = $"Command *{commandText}* was not found. Type {prefix}help to get a list of commands";
                        }
                        else if (possibleCommands.Count == 1)
                        {
                            message = $"Did you mean *{possibleCommands.First()}* ? Type {prefix}help to get a list of commands";
                        }
                        else if (possibleCommands.Count > 1 && possibleCommands.Count < 5)
                        {
                            message = $"Did you mean one of the following commands:{Environment.NewLine}{string.Join(Environment.NewLine, possibleCommands)}{Environment.NewLine}Type {prefix}help to get a list of commands";
                        }
                        else
                        {
                            message = $"{possibleCommands.Count} possible commands have been found matching your input. Please be more specific.";
                        }
                        return message;
                    }
                case CommandError.ParseFailed:
                    return "Command could not be parsed, I'm sorry :(";
                case CommandError.BadArgCount:
                    return $"Command did not have the right amount of parameters. Type {prefix}help {commandText} for more info";
                case CommandError.ObjectNotFound:
                    return "Object was not found";
                case CommandError.MultipleMatches:
                    return $"Multiple commands were found like {commandText}. Please be more specific";
                case CommandError.UnmetPrecondition:
                    return $"A precondition for the command was not met. Type {prefix}help {commandText} for more info";
                case CommandError.Exception:
                    return "An exception has occured during the command execution. My developer was notified of this";
                case CommandError.Unsuccessful:
                    return "The command excecution was unsuccessfull, I'm sorry :(";
                default:
                    break;
            }
            return "Can't execute the command!";
        }

        public async Task BuildDocumentationAsync()
        {
            string docuHTML = DocumentationBuilder.BuildDocumentation(commandService, DocumentationOutputTypes.HTML);
            string fileHTML = Path.Combine(AppContext.BaseDirectory, "documentation.html");
            await Helpers.WriteTextAsync(fileHTML, docuHTML);

            string docuMD = DocumentationBuilder.BuildDocumentation(commandService, DocumentationOutputTypes.MarkDown);
            string fileMD = Path.Combine(AppContext.BaseDirectory, "documentation.md");
            await Helpers.WriteTextAsync(fileMD, docuMD);
        }
    }
}