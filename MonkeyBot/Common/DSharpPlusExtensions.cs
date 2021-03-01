using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Threading.Tasks;

namespace DSharpPlus.CommandsNext
{
    public static class DSharpPlusExtensions
    {
        private static DiscordEmoji trashCan = DiscordEmoji.FromUnicode("🗑");

        public static async Task<DiscordMessage> RespondDeletableAsync(this CommandContext ctx, string content = null, DiscordEmbed embed = null)
        {
            DiscordMessage msg = await ctx.RespondAsync(content, embed);
            await msg.CreateReactionAsync(trashCan);
            var interactivity = ctx.Client.GetInteractivity();
            var interactivityResult = await interactivity.WaitForReactionAsync(x => x.Emoji == trashCan, msg, ctx.User, TimeSpan.FromSeconds(30));
            if (interactivityResult.TimedOut)
            {
                await msg.DeleteOwnReactionAsync(trashCan);                
            }
            else
            {
                await ctx.Message.DeleteAsync();
                await msg.DeleteAsync();
            }
            return msg;
        }

        public static async Task<DiscordMessage> OkAsync(this CommandContext ctx, string message, string title = "")
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Green)
                .WithTitle(title.IsEmptyOrWhiteSpace() ? "👍" : title)
                .WithDescription(message);
            return await ctx.RespondDeletableAsync(embed: builder.Build());
        }

        public static async Task<DiscordMessage> ErrorAsync(this CommandContext ctx, string message, string title = "")
        {            
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Red)
                .WithTitle(title.IsEmptyOrWhiteSpace() ? "🚫" : title)
                .WithDescription(message);
            return await ctx.RespondDeletableAsync(embed: builder.Build());
        }
    }
}
