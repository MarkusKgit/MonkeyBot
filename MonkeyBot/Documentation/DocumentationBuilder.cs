
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Humanizer;
using MonkeyBot.Common;
using MonkeyBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonkeyBot.Documentation
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
        public static string BuildDocumentation(CommandsNextExtension commandsNext, DocumentationOutputType outputType = DocumentationOutputType.HTML)
        {
            return outputType switch
            {
                DocumentationOutputType.HTML => BuildHtmlDocumentation(commandsNext),
                DocumentationOutputType.MarkDown => BuildMarkdownDocumentation(commandsNext),
                _ => string.Empty,
            };
        }

        private static string BuildHtmlDocumentation(CommandsNextExtension commandsNext)
            => BuildDocumentation(commandsNext, new HTMLFormatter());

        private static string BuildMarkdownDocumentation(CommandsNextExtension commandsNext)
            => BuildDocumentation(commandsNext, new MarkDownFormatter());

        private static string BuildDocumentation(CommandsNextExtension commandsNext, IDocumentFormatter f)
        {
            string prefix = GuildConfig.DefaultPrefix;
            var builder = new StringBuilder();

            var commands = commandsNext.RegisteredCommands;
            var flat = commands.SelectMany(c => c.Value is CommandGroup group ? group.Children : new Command[] { c.Value }).Distinct();
            var grouped = flat.GroupBy(c => c.Module.ModuleType);

            foreach (var group in grouped)
            {
                var module = group.Key;
                string description = module.CustomAttributes.Where(x => x.AttributeType == typeof(DescriptionAttribute)).Select(x => x.ConstructorArguments[0].Value.ToString()).FirstOrDefault();
                string moduleName = module.Name;
                builder.AppendLine(f.H2(description ?? moduleName));

                foreach (var cmd in group)
                {
                    string name = $"{(cmd.Parent != null ? cmd.Parent.Name + " " : "")}{cmd.Name}";
                    builder.AppendLine(f.H3(name));                    
                    builder.AppendLine(cmd.Description + f.NewLine(""));

                    if (cmd.Aliases.Any())
                    {
                        builder.AppendLine(f.H3("Aliases"));
                        builder.AppendLine(string.Join(" ,", cmd.Aliases) + f.NewLine(""));                        
                    }
                    
                    builder.AppendLine(f.H4("Usage"));
                    foreach (var ovl in cmd.Overloads.OrderByDescending(x => x.Priority))
                    {
                        builder.AppendLine(f.InlineCode($"{prefix}{cmd.QualifiedName} {string.Join(" ", ovl.Arguments.Select(arg => arg.IsOptional ? $"({arg.Name})" : arg.Name))}") + f.NewLine(""));
                        builder.AppendLine(string.Join(Environment.NewLine, ovl.Arguments.Select(arg => "├ " + f.InlineCode($"{arg.Name} ({commandsNext.GetUserFriendlyTypeName(arg.Type)}): {arg.Description ?? ""}") + f.NewLine(""))));
                    }

                    if (cmd.ExecutionChecks.Any())
                    {
                        builder.AppendLine(f.H3("Required permissions"));
                        builder.AppendLine(string.Join(", ", cmd.ExecutionChecks.Select(c => c.Translate())));
                    }

                    if (cmd.CustomAttributes.OfType<ExampleAttribute>().Any())
                    {
                        var examples = cmd.CustomAttributes.OfType<ExampleAttribute>().Select(e => e.ExampleText).ToList();
                        builder.AppendLine(f.H3("Example usage"));
                        builder.AppendLine(string.Join("\n", examples.Select(e => f.InlineCode($"{prefix}{e}"))));
                    }
                }
                builder.AppendLine();
                builder.AppendLine(f.NewLine(f.HorizontalRule()));
                builder.AppendLine();
            }  
            return builder.ToString();
        }
    }
}