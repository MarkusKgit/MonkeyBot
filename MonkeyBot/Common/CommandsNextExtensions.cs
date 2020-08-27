using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System;
using System.Threading.Tasks;

namespace DSharpPlus.CommandsNext
{
    public static class CommandsNextExtensions
    {
        private static DiscordEmoji trashCan = DiscordEmoji.FromUnicode("🗑");

        public static async Task<DiscordMessage> RespondDeletableAsync(this CommandContext ctx, string content = null, bool isTTS = false, DiscordEmbed embed = null)
        {
            DiscordMessage msg = await ctx.RespondAsync(content, isTTS, embed).ConfigureAwait(false);
            await msg.CreateReactionAsync(trashCan).ConfigureAwait(false);
            var interactivity = ctx.Client.GetInteractivity();
            var interactivityResult = await interactivity.WaitForReactionAsync(x => x.Emoji == trashCan, msg, ctx.User, TimeSpan.FromSeconds(30)).ConfigureAwait(false);
            if (interactivityResult.TimedOut )
            {
                await msg.DeleteOwnReactionAsync(trashCan).ConfigureAwait(false);                
            }
            else
            {
                await ctx.Message.DeleteAsync().ConfigureAwait(false);
                await msg.DeleteAsync().ConfigureAwait(false);
            }
            return msg;
        }

        public static async Task<DiscordMessage> OkAsync(this CommandContext ctx, string message, string title = "")
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Green)
                .WithTitle(title.IsEmptyOrWhiteSpace() ? "👍" : title)
                .WithDescription(message);
            return await ctx.RespondDeletableAsync(embed: builder.Build()).ConfigureAwait(false);
        }

        public static async Task<DiscordMessage> ErrorAsync(this CommandContext ctx, string message, string title = "")
        {            
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Red)
                .WithTitle(title.IsEmptyOrWhiteSpace() ? "🚫" : title)
                .WithDescription(message);
            return await ctx.RespondDeletableAsync(embed: builder.Build()).ConfigureAwait(false);
        }
    }
}
