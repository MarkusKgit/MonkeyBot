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
        public static void BuildHtmlDocumentation(CommandService commandService)
        {
            string prefix = Configuration.Load().Prefix;
            StringBuilder builder = new StringBuilder();
            
            foreach (var module in commandService.Modules)
            {
                string preconditions = string.Empty;
                foreach (var precondition in module.Preconditions)
                {
                    if (precondition is MinPermissionsAttribute)
                        preconditions += (precondition as MinPermissionsAttribute).AccessLevel.ToString();
                }
                builder.AppendLine($"<strong><ul>Module: {module.Name}</ul></strong>");
                if (!string.IsNullOrEmpty(preconditions))
                    builder.AppendLine($"Preconditions: {preconditions}");
                //string description = null;
                //foreach (var cmd in module.Commands)
                //{
                //    string parameters = string.Empty;
                //    if (cmd.Parameters != null && cmd.Parameters.Count > 0)
                //        parameters = "*" + cmd.Parameters.Select(x => x.Name).Aggregate((a, b) => (a + " " + b)) + "*";
                //    description += $"{prefix}{cmd.Aliases.First()}  {parameters}{Environment.NewLine}";
                //}

                //if (!string.IsNullOrWhiteSpace(description))
                //{
                //    builder.AddField(x =>
                //    {
                //        x.Name = module.Name;
                //        x.Value = description;
                //        x.IsInline = false;
                //    });
                //}
            }
            var result = builder.ToString();
        }
    }
}
