
using Humanizer;
using MonkeyBot.Common;
using MonkeyBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonkeyBot.Documentation
{
    //TODO: Reimplement
    
    /// <summary>
    /// Helper class to automatically build documentation based on the implemented Modules
    /// </summary>
    public static class DocumentationBuilder
    {
        ///// <summary>
        ///// Automatically build documentation of the implemented modules
        ///// Returns a formatted string according to the outputType
        ///// Currently HTML and markdown are supported
        ///// </summary>
        //public static string BuildDocumentation(CommandService commandService, DocumentationOutputType outputType = DocumentationOutputType.HTML)
        //{
        //    return outputType switch
        //    {
        //        DocumentationOutputType.HTML => BuildHtmlDocumentation(commandService),
        //        DocumentationOutputType.MarkDown => BuildMarkdownDocumentation(commandService),
        //        _ => string.Empty,
        //    };
        //}

        //private static string BuildHtmlDocumentation(CommandService commandService)
        //    => BuildDocumentation(commandService, new HTMLFormatter());

        //private static string BuildMarkdownDocumentation(CommandService commandService)
        //    => BuildDocumentation(commandService, new MarkDownFormatter());

        //private static string BuildDocumentation(CommandService commandService, IDocumentFormatter f)
        //{
        //    string prefix = GuildConfig.DefaultPrefix;
        //    var builder = new StringBuilder();

        //    foreach (ModuleInfo module in commandService.Modules)
        //    {
        //        _ = builder.AppendLine(f.H3(module.Name));
        //        List<string> modulePreconditions = module.Preconditions?.Select(x => TranslatePrecondition(x, f)).ToList();
        //        if (modulePreconditions != null && modulePreconditions.Count > 0)
        //        {
        //            _ = builder.AppendLine(f.NewLine($"{f.Strong("Preconditions:")} {string.Join(", ", modulePreconditions)}"));
        //        }
        //        _ = builder.AppendLine(f.NewLine(""));
        //        foreach (CommandInfo cmd in module.Commands)
        //        {
        //            string parameters = string.Empty;
        //            if (cmd.Parameters != null && cmd.Parameters.Count > 0)
        //            {
        //                parameters = $"{string.Join(" ", cmd.Parameters.Select(x => $"_{x.Name}"))}";
        //            }
        //            _ = builder.AppendLine(f.NewLine(f.InlineCode($"{prefix}{cmd.Aliases[0]} {parameters}")));
        //            ExampleAttribute example = cmd.Attributes.OfType<ExampleAttribute>().FirstOrDefault();
        //            if (example != null && !example.ExampleText.IsEmpty())
        //            {
        //                _ = builder.AppendLine(f.NewLine($"{f.Em("Example:")} {f.InlineCode(example.ExampleText)}"));
        //            }
        //            List<string> commandPreconditions = cmd.Preconditions?.Select(x => TranslatePrecondition(x, f)).ToList();
        //            if (commandPreconditions != null && commandPreconditions.Count > 0)
        //            {
        //                _ = builder.AppendLine(f.NewLine($"{f.Em("Preconditions:")} {string.Join(", ", commandPreconditions)}"));
        //            }
        //            if (!cmd.Remarks.IsEmpty())
        //            {
        //                _ = builder.AppendLine(f.NewLine($"{f.Em("Remarks:")} {cmd.Remarks}"));
        //            }
        //            _ = builder.AppendLine(f.NewLine(""));
        //        }
        //        _ = builder.AppendLine(f.NewLine(f.HorizontalRule()));
        //    }
        //    return builder.ToString();
        //}

        //private static string TranslatePrecondition(PreconditionAttribute precondition, IDocumentFormatter f)
        //{
        //    if (precondition is MinPermissionsAttribute minPermissionsAttribute)
        //    {
        //        return $"Minimum permission: {f.Em($"{minPermissionsAttribute.AccessLevel.Humanize(LetterCasing.Title)}")}";
        //    }
        //    else if (precondition is RequireContextAttribute contextAttribute)
        //    {
        //        return $"Can only be used in a {f.Em(TranslateContext(contextAttribute.Contexts))}";
        //    }
        //    else if (precondition is RequireBotPermissionAttribute || precondition is RequireUserPermissionAttribute)
        //    {
        //        string permission = "";
        //        string prefix = "";
        //        GuildPermission? guildPermission;
        //        ChannelPermission? channelPermission;
        //        if (precondition is RequireBotPermissionAttribute)
        //        {
        //            guildPermission = (precondition as RequireBotPermissionAttribute).GuildPermission;
        //            channelPermission = (precondition as RequireBotPermissionAttribute).ChannelPermission;
        //            prefix = "Bot";
        //        }
        //        else
        //        {
        //            guildPermission = (precondition as RequireUserPermissionAttribute).GuildPermission;
        //            channelPermission = (precondition as RequireUserPermissionAttribute).ChannelPermission;
        //            prefix = "User";
        //        }
        //        if (guildPermission != null && guildPermission.HasValue)
        //        {
        //            List<GuildPermission> guildPermissions = guildPermission.Value.ToString()
        //                .Split(',')
        //                .Select(flag => (GuildPermission)Enum.Parse(typeof(GuildPermission), flag))
        //                .ToList();
        //            permission += $"{prefix} requires guild permission{(guildPermissions.Count > 1 ? "s" : "")}: {f.Em(string.Join(", ", guildPermissions.Select(gp => gp.Humanize(LetterCasing.Title))))} ";
        //        }
        //        if (channelPermission != null && channelPermission.HasValue)
        //        {
        //            List<ChannelPermission> channelPermissions = channelPermission.Value.ToString()
        //                .Split(',')
        //                .Select(flag => (ChannelPermission)Enum.Parse(typeof(ChannelPermission), flag))
        //                .ToList();
        //            permission += $"{prefix} requires channel permission{(channelPermissions.Count > 1 ? "s" : "")}: {f.Em(string.Join(", ", channelPermissions.Select(cp => cp.Humanize(LetterCasing.Title))))} ";
        //        }
        //        return permission.Trim();
        //    }
        //    else
        //    {
        //        return precondition.ToString();
        //    }
        //}

        //private static string TranslateContext(ContextType context)
        //{
        //    return context switch
        //    {
        //        ContextType.Guild => "channel",
        //        ContextType.DM => "private message",
        //        ContextType.Group => "private group",
        //        _ => "",
        //    };
        //}
    }
}