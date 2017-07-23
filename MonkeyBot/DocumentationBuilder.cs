using Discord;
using Discord.Commands;
using MonkeyBot.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonkeyBot.Preconditions;

namespace MonkeyBot
{
    public static class DocumentationBuilder
    {
        public static string BuildHtmlDocumentation(CommandService commandService)
        {
            string prefix = Configuration.Load().Prefix;
            StringBuilder builder = new StringBuilder();
            
            foreach (var module in commandService.Modules)
            {
                List<string> preconditions = new List<string>();
                foreach (var precondition in module.Preconditions)
                {
                    if (precondition is MinPermissionsAttribute)
                        preconditions.Add($"Minimum permission: <em>{(precondition as MinPermissionsAttribute).AccessLevel.ToString()}</em>");
                    else if (precondition is RequireContextAttribute)
                        preconditions.Add($"Can only be used in a <em>{(precondition as RequireContextAttribute).Contexts.ToString()}</em> context");
                    else
                        preconditions.Add(precondition.ToString());
                }
                builder.AppendLine($"<strong>Module: {module.Name}</strong>");
                if (preconditions.Count > 0)
                    builder.AppendLine($"Preconditions: {string.Join(", ", preconditions)}");                
                foreach (var cmd in module.Commands)
                {
                    
                    string parameters = string.Empty;
                    if (cmd.Parameters != null && cmd.Parameters.Count > 0)
                        parameters = $"<em>{cmd.Parameters.Select(x => x.Name).Aggregate((a, b) => (a + " " + b))}</em>";
                    builder.AppendLine($"<strong>{prefix}{cmd.Aliases.First()}</strong>  {parameters}");
                    if (!string.IsNullOrEmpty(cmd.Remarks))
                        builder.AppendLine(cmd.Remarks);
                }
                builder.AppendLine("");                               
            }            
            return builder.ToString();
        }
    }
}
