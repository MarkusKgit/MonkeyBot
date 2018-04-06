using Discord;
using Discord.Commands;
using dokas.FluentStrings;
using MonkeyBot.Common;
using MonkeyBot.Modules;
using MonkeyBot.Preconditions;
using System;
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
                        parameters = $"{string.Join(" ", cmd.Parameters.Select(x => $"_{x.Name}"))}";
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
                var contextAttribute = precondition as RequireContextAttribute;
                string context = TranslateContext(contextAttribute.Contexts);
                return $"Can only be used in a {f.Em(context)}";
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
                    permission += $"{prefix} requires guild permission{(guildPermissions.Count() > 1 ? "s" : "")}: {f.Em(string.Join(", ", guildPermissions.Select(x => TranslateGuildPermission(x))))} ";
                }
                if (channelPermission != null && channelPermission.HasValue)
                {
                    var channelPermissions = channelPermission.Value.ToString().Split(',').Select(flag => (ChannelPermission)Enum.Parse(typeof(ChannelPermission), flag)).ToList();
                    permission += $"{prefix} requires channel permission{(channelPermissions.Count() > 1 ? "s" : "")}: {f.Em(string.Join(", ", channelPermissions.Select(x => TranslateChannelPermission(x))))} ";
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

        private static string TranslateGuildPermission(GuildPermission guildPermission)
        {
            switch (guildPermission)
            {
                case GuildPermission.CreateInstantInvite:
                    return "Create Instant Invite";
                case GuildPermission.KickMembers:
                    return "Kick Members";
                case GuildPermission.BanMembers:
                    return "Ban Members";
                case GuildPermission.Administrator:
                    return "Administrator";
                case GuildPermission.ManageChannels:
                    return "Manage Channels";
                case GuildPermission.ManageGuild:
                    return "Manage Guild";
                case GuildPermission.AddReactions:
                    return "Add Reactions";
                case GuildPermission.ReadMessages:
                    return "Read Messages";
                case GuildPermission.SendMessages:
                    return "Send Messages";
                case GuildPermission.SendTTSMessages:
                    return "Send TTS Messages";
                case GuildPermission.ManageMessages:
                    return "Manage Messages";
                case GuildPermission.EmbedLinks:
                    return "Embed Links";
                case GuildPermission.AttachFiles:
                    return "Attach Files";
                case GuildPermission.ReadMessageHistory:
                    return "Read Message History";
                case GuildPermission.MentionEveryone:
                    return "Mention Everyone";
                case GuildPermission.UseExternalEmojis:
                    return "Use External Emojis";
                case GuildPermission.Connect:
                    return "Connect";
                case GuildPermission.Speak:
                    return "Speak";
                case GuildPermission.MuteMembers:
                    return "Mute Members";
                case GuildPermission.DeafenMembers:
                    return "Deafen Members";
                case GuildPermission.MoveMembers:
                    return "Move Members";
                case GuildPermission.UseVAD:
                    return "Use VAD";
                case GuildPermission.ChangeNickname:
                    return "Change Nickname";
                case GuildPermission.ManageNicknames:
                    return "Manage Nicknames";
                case GuildPermission.ManageRoles:
                    return "Manage Roles";
                case GuildPermission.ManageWebhooks:
                    return "Manage Webhooks";
                case GuildPermission.ManageEmojis:
                    return "Manage Emojis";
                default:
                    return guildPermission.ToString();
            }
        }

        private static string TranslateChannelPermission(ChannelPermission channelPermission)
        {
            switch (channelPermission)
            {
                case ChannelPermission.CreateInstantInvite:
                    return "Create Instant Invite";
                case ChannelPermission.ManageChannel:
                    return "Manage Channel";
                case ChannelPermission.AddReactions:
                    return "Add Reactions";
                case ChannelPermission.ReadMessages:
                    return "Read Messages";
                case ChannelPermission.SendMessages:
                    return "Send Messages";
                case ChannelPermission.SendTTSMessages:
                    return "Send TTS Messages";
                case ChannelPermission.ManageMessages:
                    return "Manage Messages";
                case ChannelPermission.EmbedLinks:
                    return "Embed Links";
                case ChannelPermission.AttachFiles:
                    return "Attach Files";
                case ChannelPermission.ReadMessageHistory:
                    return "Read Message History";
                case ChannelPermission.MentionEveryone:
                    return "Mention Everyone";
                case ChannelPermission.UseExternalEmojis:
                    return "Use External Emojis";
                case ChannelPermission.Connect:
                    return "Connect";
                case ChannelPermission.Speak:
                    return "Speak";
                case ChannelPermission.MuteMembers:
                    return "Mute Members";
                case ChannelPermission.DeafenMembers:
                    return "Deafen Members";
                case ChannelPermission.MoveMembers:
                    return "Move Members";
                case ChannelPermission.UseVAD:
                    return "Use VAD";
                case ChannelPermission.ManagePermissions:
                    return "Manage Permissions";
                case ChannelPermission.ManageWebhooks:
                    return "Manage Webhooks";
                default:
                    return channelPermission.ToString();
            }
        }

        public enum OutputTypes
        {
            HTML,
            MarkDown
        }
    }
}