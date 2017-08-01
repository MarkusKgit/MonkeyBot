using Discord.Commands;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonkeyBot
{
    public static class DocumentationBuilder
    {
        public static string BuildHtmlDocumentationAsync(CommandService commandService)
        {
            string prefix = Configuration.DefaultPrefix;
            StringBuilder builder = new StringBuilder();

            foreach (var module in commandService.Modules)
            {
                List<string> preconditions = new List<string>();
                foreach (var precondition in module.Preconditions)
                {
                    preconditions.Add(TranslatePrecondition(precondition));
                }
                builder.AppendLine($"<strong>Module: {module.Name}</strong>");
                if (preconditions.Count > 0)
                    builder.AppendLine($"Module Preconditions: {string.Join(", ", preconditions)}");
                foreach (var cmd in module.Commands)
                {
                    string parameters = string.Empty;
                    if (cmd.Parameters != null && cmd.Parameters.Count > 0)
                        parameters = $"<em>{cmd.Parameters.Select(x => x.Name).Aggregate((a, b) => (a + " " + b))}</em>";
                    builder.AppendLine($"<strong>{prefix}{cmd.Aliases.First()}</strong>  {parameters}");
                    preconditions.Clear();
                    foreach (var precondition in cmd.Preconditions)
                    {
                        preconditions.Add(TranslatePrecondition(precondition));
                    }
                    if (preconditions.Count > 0)
                        builder.AppendLine($"Command Preconditions: {string.Join(", ", preconditions)}");
                    if (!string.IsNullOrEmpty(cmd.Remarks))
                        builder.AppendLine(cmd.Remarks);
                }
                builder.AppendLine("");
            }
            return builder.ToString();
        }

        private static string TranslatePrecondition(PreconditionAttribute precondition)
        {
            if (precondition is MinPermissionsAttribute)
                return $"Minimum permission: <em>{(precondition as MinPermissionsAttribute).AccessLevel.ToString()}</em>";
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
                return $"Can only be used in a <em>{context}</em>";
            }
            else
                return precondition.ToString();
        }
    }
}