using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MonkeyBot.Common;
using MonkeyBot.Database;
using MonkeyBot.Documentation;
using MonkeyBot.Models;
using MonkeyBot.Services;
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
        private readonly CommandService commandService;
        private readonly MonkeyDBContext dbContext;

        /// <summary>
        /// Create a new CommandManager instance with DI. Use <see cref="StartAsync"/> afterwards to actually start the CommandManager/>
        /// </summary>
        public CommandManager(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            discordClient = serviceProvider.GetRequiredService<DiscordSocketClient>();
            commandService = serviceProvider.GetRequiredService<CommandService>();
            dbContext = serviceProvider.GetRequiredService<MonkeyDBContext>();
        }

        public async Task StartAsync()
        {
            _ = await commandService.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider).ConfigureAwait(false);

            discordClient.MessageReceived += HandleCommandAsync;
        }

        public Task<string> GetPrefixAsync(IGuild guild) => GetPrefixAsync(guild?.Id);

        public async Task<string> GetPrefixAsync(ulong? guildId)
        {
            return guildId == null
                ? GuildConfig.DefaultPrefix
                : (await dbContext.GuildConfigs.SingleOrDefaultAsync(c => c.GuildID == guildId.Value).ConfigureAwait(false))?.CommandPrefix
                    ?? GuildConfig.DefaultPrefix;
        }

        private async Task HandleCommandAsync(SocketMessage socketMsg)
        {
            if (!(socketMsg is SocketUserMessage msg))
            {
                return;
            }

            var context = new SocketCommandContext(discordClient, msg);
            SocketGuild guild = (msg.Channel as SocketTextChannel)?.Guild;
            string prefix = await GetPrefixAsync(guild?.Id).ConfigureAwait(false);
            int argPos = 0;

            if (msg.HasStringPrefix(prefix, ref argPos))
            {
                string commandText = msg.Content.Substring(argPos).ToLowerInvariant().Trim();
                if (!commandText.IsEmpty())
                {
                    IResult result = await commandService.ExecuteAsync(context, argPos, serviceProvider).ConfigureAwait(false);

                    if (!result.IsSuccess)
                    {
                        if (result.Error.HasValue)
                        {
                            CommandError error = result.Error.Value;
                            string errorMessage = GetCommandErrorMessage(error, prefix, commandText);
                            _ = await context.Channel.SendMessageAsync(errorMessage).ConfigureAwait(false);
                            if (error == CommandError.Exception || error == CommandError.ParseFailed || error == CommandError.Unsuccessful)
                            {
                                if (discordClient is MonkeyClient monkeyClient)
                                {
                                    await monkeyClient.NotifyAdminAsync(errorMessage).ConfigureAwait(false);
                                }
                            }
                        }
                        else
                        {
                            _ = await context.Channel.SendMessageAsync(result.ToString()).ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        private string GetCommandErrorMessage(CommandError error, string prefix, string commandText)
        {
            return error switch
            {
                CommandError.UnknownCommand => GetPossibleCommands(prefix, commandText),
                CommandError.ParseFailed => "Command could not be parsed, I'm sorry :(",
                CommandError.BadArgCount => $"Command did not have the right amount of parameters. Type {prefix}help {commandText} for more info",
                CommandError.ObjectNotFound => " was not found",
                CommandError.MultipleMatches => $"Multiple commands were found like {commandText}. Please be more specific",
                CommandError.UnmetPrecondition => $"A precondition for the command was not met. Type {prefix}help {commandText} for more info",
                CommandError.Exception => "An exception has occured during the command execution. My developer was notified of this",
                CommandError.Unsuccessful => "The command excecution was unsuccessfull, I'm sorry :(",
                _ => "Unknown Command Error"
            };
        }

        private string GetPossibleCommands(string prefix, string commandText)
        {
            List<string> possibleCommands =
                        commandService
                            .Modules
                            .SelectMany(module => module.Commands)
                            .SelectMany(command => command.Aliases.Select(a => $"{prefix}{a}"))
                            .Distinct()
                            .Where(alias => alias.Contains(commandText, StringComparison.OrdinalIgnoreCase))
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

        public async Task BuildDocumentationAsync()
        {
            string docuHTML = DocumentationBuilder.BuildDocumentation(commandService, DocumentationOutputType.HTML);
            string fileHTML = Path.Combine(AppContext.BaseDirectory, "documentation.html");
            await MonkeyHelpers.WriteTextAsync(fileHTML, docuHTML).ConfigureAwait(false);

            string docuMD = DocumentationBuilder.BuildDocumentation(commandService, DocumentationOutputType.MarkDown);
            string fileMD = Path.Combine(AppContext.BaseDirectory, "documentation.md");
            await MonkeyHelpers.WriteTextAsync(fileMD, docuMD).ConfigureAwait(false);
        }
    }
}