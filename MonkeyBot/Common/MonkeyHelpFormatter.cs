using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;
using MonkeyBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyBot.Common
{
    public class MonkeyHelpFormatter : BaseHelpFormatter
    {
        private readonly DiscordEmbedBuilder _embedBuilder;
        private Command _specificCommand;
        private string prefix = "!";

        public MonkeyHelpFormatter(CommandContext ctx, IGuildService guildService) : base(ctx)
        {
            _embedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Help")
                .WithColor(DiscordColor.SpringGreen);

            if (ctx.Guild != null)
            {
                prefix = guildService?.GetOrCreateConfigAsync(ctx.Guild.Id)?.Result?.CommandPrefix ?? prefix;
            }
        }

        public override BaseHelpFormatter WithCommand(Command command)
        {
            _specificCommand = command;

            _embedBuilder.WithDescription($"{Formatter.InlineCode(command.Name)}: {command.Description ?? "No description provided."}");

            if (command is CommandGroup cgroup && cgroup.IsExecutableWithoutSubcommands)
            {
                _embedBuilder.WithDescription($"{_embedBuilder.Description}\n\nThis group can be executed as a standalone command.");
            }

            if (command.Aliases?.Any() == true)
            {
                _embedBuilder.AddField("Aliases", string.Join(", ", command.Aliases.Select(Formatter.InlineCode)), false);
            }

            if (command.Overloads?.Any() == true)
            {
                var sb = new StringBuilder();

                foreach (var ovl in command.Overloads.OrderByDescending(x => x.Priority))
                {
                    sb.Append('`').Append(command.QualifiedName);

                    foreach (var arg in ovl.Arguments)
                    {
                        sb.Append(arg.IsOptional || arg.IsCatchAll ? " [" : " <").Append(arg.Name).Append(arg.IsCatchAll ? "..." : "").Append(arg.IsOptional || arg.IsCatchAll ? ']' : '>');
                    }

                    sb.Append("`\n");

                    foreach (var arg in ovl.Arguments)
                    {
                        sb.Append('`').Append(arg.Name).Append(" (").Append(this.CommandsNext.GetUserFriendlyTypeName(arg.Type)).Append(")`: ").Append(arg.Description ?? "No description provided.").Append('\n');
                    }

                    sb.Append('\n');
                }

                _embedBuilder.AddField("Arguments", sb.ToString().Trim(), false);
            }

            _embedBuilder.AddField("Requires permissions", string.Join(", ", command.ExecutionChecks.Select(c => c.Translate())));

            if ( command.CustomAttributes.OfType<ExampleAttribute>().Any())
            {
                var examples = command.CustomAttributes.OfType<ExampleAttribute>().Select(e => e.ExampleText).ToList();
                _embedBuilder.AddField("Usage example(s)", string.Join("\n", examples), false);
                //TODO: Replace default prefix with specific
            }

            return this;
        }

        public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
        {
            _embedBuilder.AddField(
                _specificCommand != null ? "Subcommands" : "Commands", 
                string.Join(", ", subcommands.Select(x => Formatter.InlineCode(x.Name))), false);
            return this;
        }

        public override CommandHelpMessage Build()
        {
            if (_specificCommand == null)
            {
                _embedBuilder.WithDescription($"Here is a list of all commands and groups. Type {prefix}{Formatter.Italic("commandname")} for more information about a specific command.");
            }

            return new CommandHelpMessage(embed: _embedBuilder.Build());
        }
    }
}
