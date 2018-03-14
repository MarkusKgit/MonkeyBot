using Discord.Commands;
using dokas.FluentStrings;
using MonkeyBot.Common;
using MonkeyBot.Modules;
using MonkeyBot.Preconditions;
using System.Linq;
using System.Text;

namespace MonkeyBot.Utilities
{
    public static class DocumentationBuilder
    {
        public static string BuildDocumentation(CommandService commandService, OutputTypes outputType = OutputTypes.HTML)
        {
            switch (outputType)
            {
                case OutputTypes.HTML:
                    return BuildHtmlDocumentation(commandService);
                case OutputTypes.MarkDown:
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
                builder.AppendLine(f.H2(module.Name));
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
                        parameters = $"{string.Join(" ", cmd.Parameters.Select(x => x.Name))}";
                    //builder.AppendLine(f.NewLine($"{f.Strong($"{prefix}{cmd.Aliases.First()}")} {parameters}"));
                    builder.AppendLine(f.NewLine(f.InlineCode($"{prefix}{cmd.Aliases.First()} {parameters}")));
                    var example = cmd.Attributes.OfType<ExampleAttribute>().FirstOrDefault();
                    if (example != null && !example.ExampleText.IsEmpty())
                        builder.AppendLine(f.NewLine($"{f.Em("Example:")} {f.InlineCode(example.ExampleText)}"));
                    var commandPreconditions = cmd.Preconditions?.Select(x => TranslatePrecondition(x, f)).ToList();
                    if (commandPreconditions != null && commandPreconditions.Count > 0)
                        builder.AppendLine(f.NewLine($"{f.Em("Preconditions:")} {string.Join(", ", commandPreconditions)}"));
                    if (!cmd.Remarks.IsEmpty())
                        builder.AppendLine(f.NewLine($"{f.Em("Remarks:")} {cmd.Remarks}"));
                    builder.AppendLine(f.NewLine(""));
                }
            }
            return builder.ToString();
        }

        private static string TranslatePrecondition(PreconditionAttribute precondition, IDocumentFormatter f)
        {
            if (precondition is MinPermissionsAttribute)
                return $"Minimum permission: {f.Em($"{(precondition as MinPermissionsAttribute).AccessLevel.ToString()}")}";
            else if (precondition is RequireContextAttribute)
            {
                string context = string.Empty;
                var contextAttribute = precondition as RequireContextAttribute;
                switch (contextAttribute.Contexts)
                {
                    case ContextType.Guild:
                        context = "channel";
                        break;

                    case ContextType.DM:
                        context = "private message";
                        break;

                    case ContextType.Group:
                        context = "private group";
                        break;

                    default:
                        break;
                }
                return $"Can only be used in a {f.Em(context)}";
            }
            else
                return precondition.ToString();
        }

        public enum OutputTypes
        {
            HTML,
            MarkDown
        }
    }
}