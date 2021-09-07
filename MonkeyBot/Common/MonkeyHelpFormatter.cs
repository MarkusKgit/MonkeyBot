using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;
using MonkeyBot.Models;
using MonkeyBot.Services;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonkeyBot.Common
{
    public class MonkeyHelpFormatter : BaseHelpFormatter
    {
        private readonly DiscordEmbedBuilder _embedBuilder;
        private Command _specificCommand;
        private readonly string prefix = GuildConfig.DefaultPrefix;

        public MonkeyHelpFormatter(CommandContext ctx, IGuildService guildService) : base(ctx)
        {
            _embedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Help")
                .WithColor(DiscordColor.SpringGreen);

            if (ctx.Guild != null && guildService != null)
            {
                prefix = guildService.GetPrefixForGuild(ctx.Guild.Id).GetAwaiter().GetResult();
            }
        }

        public override BaseHelpFormatter WithCommand(Command command)
        {
            _specificCommand = command;

            _embedBuilder.WithDescription($"{Formatter.InlineCode(prefix + command.Name)}: {command.Description ?? "No description provided."}");

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
                    sb.Append('`').Append(prefix + command.QualifiedName);

                    if (ovl.Arguments.Any())
                    {
                        foreach (var arg in ovl.Arguments)
                        {
                            sb.Append(arg.IsOptional || arg.IsCatchAll ? " [" : " <").Append(arg.Name).Append(arg.IsCatchAll ? "..." : "").Append(arg.IsOptional || arg.IsCatchAll ? ']' : '>');
                        }

                        sb.Append("`\n");

                        foreach (var arg in ovl.Arguments)
                        {
                            sb.Append('`').Append("├ ").Append(arg.Name).Append(" (").Append(CommandsNext.GetUserFriendlyTypeName(arg.Type)).Append(")`: ").Append(arg.Description ?? "No description provided.").Append('\n');
                        }
                    }
                    else 
                    {
                        sb.Append(" <none>`");
                    }

                    sb.Append('\n');
                }

                _embedBuilder.AddField("Arguments", sb.ToString().Trim(), false);
            }

            if (command.ExecutionChecks.Any())
            {
                _embedBuilder.AddField("Requires permissions", string.Join(", ", command.ExecutionChecks.Select(c => c.Translate())));
            }

            if (command.CustomAttributes.OfType<ExampleAttribute>().Any())
            {
                var examples = command.CustomAttributes.OfType<ExampleAttribute>().Select(e => e.ExampleText).ToList();
                _embedBuilder.AddField("Usage example(s)", string.Join("\n", examples.Select(e => $"{prefix}{e}")), false);
            }

            return this;
        }

        public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
        {
            if (_specificCommand != null)
            {
                // -> Subcommands
                _embedBuilder.AddField("Subcommands", string.Join(", ", subcommands.Select(x => Formatter.InlineCode(prefix + x.Name))), false);
            }
            else
            {
                var descriptions = subcommands.Select(GetCommandDescription).ToList();
                var groupedByModule = subcommands.GroupBy(cmd => GetCommandDescription(cmd));
                foreach (var grp in groupedByModule)
                {
                    _embedBuilder.AddField(grp.Key, string.Join(", ", grp.Select(x => Formatter.InlineCode(prefix + x.Name))));
                }
                //_embedBuilder.AddField("Commands", string.Join("\n", groupedByModule.Select(grp => $"{grp.Key}:\n{string.Join(", ", grp.Select(x => Formatter.InlineCode(prefix + x.Name)))}")));
            }
            return this;
        }

        private static string GetCommandDescription(Command cmd) 
            => cmd.Module.ModuleType.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(DescriptionAttribute))?.ConstructorArguments?.First().Value.ToString() ?? cmd.Module.ModuleType.Name;

        public override CommandHelpMessage Build()
        {
            if (_specificCommand == null)
            {
                _embedBuilder.WithDescription($"Here is a list of all commands and groups. Type {prefix}help {Formatter.Italic("commandname")} for more information about a specific command.");
            }

            return new CommandHelpMessage(embed: _embedBuilder.Build());
        }
    }
}
