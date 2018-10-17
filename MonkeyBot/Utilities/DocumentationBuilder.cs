using Discord;
using Discord.Commands;
using dokas.FluentStrings;
using Humanizer;
using MonkeyBot.Common;
using MonkeyBot.Modules;
using MonkeyBot.Preconditions;
using System;
using System.Linq;
using System.Text;

namespace MonkeyBot.Utilities
{
    /// <summary>
    /// Helper class to automatically build documentation based on the implemented Modules
    /// </summary>
    public static class DocumentationBuilder
    {
        /// <summary>
        /// Automatically build documentation of the implemented modules
        /// Returns a formatted string according to the outputType
        /// Currently HTML and markdown are supported
        /// </summary>
        public static string BuildDocumentation(CommandService commandService, DocumentationOutputTypes outputType = DocumentationOutputTypes.HTML)
        {
            switch (outputType)
            {
                case DocumentationOutputTypes.HTML:
                    return BuildHtmlDocumentation(commandService);
                case DocumentationOutputTypes.MarkDown:
                    return BuildMarkdownDocumentation(commandService);
                default:
                    return string.Empty;
            }
        }

        private static string BuildHtmlDocumentation(CommandService commandService)
        {
            return BuildDocumentation(commandService, new HTMLFormatter());
        }

        private static string BuildMarkdownDocumentation(CommandService commandService)
        {
            return BuildDocumentation(commandService, new MarkDownFormatter());
        }

        private static string BuildDocumentation(CommandService commandService, IDocumentFormatter f)
        {
            string prefix = Configuration.DefaultPrefix;
            StringBuilder builder = new StringBuilder();

            foreach (var module in commandService.Modules)
            {
                builder.AppendLine(f.H3(module.Name));
                var modulePreconditions = module.Preconditions?.Select(x => TranslatePrecondition(x, f)).ToList();
                if (modulePreconditions != null && modulePreconditions.Count > 0)
                {
                    builder.AppendLine(f.NewLine($"{f.Strong("Preconditions:")} {string.Join(", ", modulePreconditions)}"));
                }
                builder.AppendLine(f.NewLine(""));
                foreach (var cmd in module.Commands)
                {
                    string parameters = string.Empty;
                    if (cmd.Parameters != null && cmd.Parameters.Count > 0)
                    {
                        parameters = $"{string.Join(" ", cmd.Parameters.Select(x => $"_{x.Name}"))}";
                    }
                    builder.AppendLine(f.NewLine(f.InlineCode($"{prefix}{cmd.Aliases.First()} {parameters}")));
                    var example = cmd.Attributes.OfType<ExampleAttribute>().FirstOrDefault();
                    if (example != null && !example.ExampleText.IsEmpty())
                    {
                        builder.AppendLine(f.NewLine($"{f.Em("Example:")} {f.InlineCode(example.ExampleText)}"));
                    }
                    var commandPreconditions = cmd.Preconditions?.Select(x => TranslatePrecondition(x, f)).ToList();
                    if (commandPreconditions != null && commandPreconditions.Count > 0)
                    {
                        builder.AppendLine(f.NewLine($"{f.Em("Preconditions:")} {string.Join(", ", commandPreconditions)}"));
                    }
                    if (!cmd.Remarks.IsEmpty())
                    {
                        builder.AppendLine(f.NewLine($"{f.Em("Remarks:")} {cmd.Remarks}"));
                    }
                    builder.AppendLine(f.NewLine(""));
                }
                builder.AppendLine(f.NewLine(f.HorizontalRule()));
            }
            return builder.ToString();
        }

        private static string TranslatePrecondition(PreconditionAttribute precondition, IDocumentFormatter f)
        {
            if (precondition is MinPermissionsAttribute minPermissionsAttribute)
            {
                return $"Minimum permission: {f.Em($"{minPermissionsAttribute.AccessLevel.Humanize(LetterCasing.Title)}")}";
            }
            else if (precondition is RequireContextAttribute contextAttribute)
            {
                return $"Can only be used in a {f.Em(TranslateContext(contextAttribute.Contexts))}";
            }
            else if (precondition is RequireBotPermissionAttribute || precondition is RequireUserPermissionAttribute)
            {
                string permission = "";
                string prefix = "";
                GuildPermission? guildPermission;
                ChannelPermission? channelPermission;
                if (precondition is RequireBotPermissionAttribute)
                {
                    guildPermission = (precondition as RequireBotPermissionAttribute).GuildPermission;
                    channelPermission = (precondition as RequireBotPermissionAttribute).ChannelPermission;
                    prefix = "Bot";
                }
                else
                {
                    guildPermission = (precondition as RequireUserPermissionAttribute).GuildPermission;
                    channelPermission = (precondition as RequireUserPermissionAttribute).ChannelPermission;
                    prefix = "User";
                }
                if (guildPermission != null && guildPermission.HasValue)
                {
                    var guildPermissions = guildPermission.Value.ToString().Split(',').Select(flag => (GuildPermission)Enum.Parse(typeof(GuildPermission), flag)).ToList();
                    permission += $"{prefix} requires guild permission{(guildPermissions.Count() > 1 ? "s" : "")}: {f.Em(string.Join(", ", guildPermissions.Select(gp => gp.Humanize(LetterCasing.Title))))} ";
                }
                if (channelPermission != null && channelPermission.HasValue)
                {
                    var channelPermissions = channelPermission.Value.ToString().Split(',').Select(flag => (ChannelPermission)Enum.Parse(typeof(ChannelPermission), flag)).ToList();
                    permission += $"{prefix} requires channel permission{(channelPermissions.Count() > 1 ? "s" : "")}: {f.Em(string.Join(", ", channelPermissions.Select(cp => cp.Humanize(LetterCasing.Title))))} ";
                }
                return permission.Trim();
            }
            else
                return precondition.ToString();
        }

        private static string TranslateContext(ContextType context)
        {
            switch (context)
            {
                case ContextType.Guild:
                    return "channel";

                case ContextType.DM:
                    return "private message";

                case ContextType.Group:
                    return "private group";

                default:
                    return "";
            }
        }
    }
}