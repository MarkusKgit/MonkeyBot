using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Humanizer;
using MonkeyBot.Common;
using System;
using System.Threading.Tasks;

namespace DSharpPlus.CommandsNext
{
    public static class DSharpPlusExtensions
    {
        private static readonly DiscordComponentEmoji trashCan = new(DiscordEmoji.FromUnicode("❌"));

        public static async Task<DiscordMessage> RespondDeletableAsync(this CommandContext ctx, string content = null, DiscordEmbed embed = null)
        {
            var msgBuilder = new DiscordMessageBuilder();
            msgBuilder
                .WithContent(content)
                .WithEmbed(embed)
                .AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"delete_button_{Guid.NewGuid()}", "Delete", emoji: trashCan));
            DiscordMessage msg = await ctx.RespondAsync(msgBuilder);

            var interactivity = ctx.Client.GetInteractivity();

            _ = Task.Run(async () =>
            {
                var interactivityResult = await interactivity.WaitForButtonAsync(msg, ctx.User, TimeSpan.FromSeconds(10));
                if (interactivityResult.TimedOut)
                {
                    await msg.ModifyAsync(b => b.ClearComponents());
                }
                else
                {
                    await ctx.Message.DeleteAsync();
                    await msg.DeleteAsync();
                }
            });

            return msg;
        }

        public static Task<DiscordMessage> RespondDeletableAsync(this CommandContext ctx, string content)
            => RespondDeletableAsync(ctx, content, null);

        public static Task<DiscordMessage> RespondDeletableAsync(this CommandContext ctx, DiscordEmbed embed)
            => RespondDeletableAsync(ctx, null, embed);

        public static async Task<DiscordMessage> OkAsync(this CommandContext ctx, string message, string title = "", bool deletable = true)
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Green)
                .WithDescription(message)
                .WithThumbnail("https://cdn.discordapp.com/emojis/900001540306268191.png?size=40")
                .WithTitle(!title.IsEmptyOrWhiteSpace() ? title : "Ok");
            return deletable
                ? await ctx.RespondDeletableAsync(builder.Build())
                : await ctx.RespondAsync(builder.Build());
        }

        public static async Task<DiscordMessage> ErrorAsync(this CommandContext ctx, string message, string title = "", bool deletable = true)
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Red)
                .WithDescription(message)
                .WithThumbnail("https://cdn.discordapp.com/emojis/900001540855713824.png?size=40")
                .WithTitle(!title.IsEmptyOrWhiteSpace() ? title : "Oh oh");
            return deletable
                ? await ctx.RespondDeletableAsync(builder.Build())
                : await ctx.RespondAsync(builder.Build());
        }

        public static string Translate(this CheckBaseAttribute check)
        {
            return check switch
            {
                RequireOwnerAttribute => "Requires Bot Owner",
                RequireGuildAttribute => "Can only be used in a guild (not in DMs)",
                RequireDirectMessageAttribute => "Can only be used in DMs",
                RequireBotPermissionsAttribute botperm => $"Requires Bot permission: {botperm.Permissions}",
                RequireUserPermissionsAttribute userPerm => $"Requires User permission: {userPerm.Permissions}",
                RequirePermissionsAttribute perms => $"Requires User and Bot Permission: {perms.Permissions}",
                RequireRolesAttribute roles => $"Requires role(s): {string.Join(", ", roles.RoleNames)}",
                RequireNsfwAttribute => $"Can only be run in a nsfw channel",
                CooldownAttribute cooldown => $"Has a cooldown of {cooldown.Reset.Humanize()}",
                MinPermissionsAttribute minPermissions => $"You need to be {minPermissions.AccessLevel}",
                _ => $"{check.TypeId} failed"
            };
        }
    }
}
