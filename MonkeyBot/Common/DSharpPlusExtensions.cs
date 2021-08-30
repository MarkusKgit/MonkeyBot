using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
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
                    await msg.ModifyAsync(b => { 
                        b.Clear();
                        b.WithContent(content);
                        b.WithEmbed(embed); 
                    });
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
                .WithTitle(title.IsEmptyOrWhiteSpace() ? "👍" : title)
                .WithDescription(message);
            return deletable 
                ? await ctx.RespondDeletableAsync(builder.Build())
                : await ctx.RespondAsync(builder.Build());
        }

        public static async Task<DiscordMessage> ErrorAsync(this CommandContext ctx, string message, string title = "", bool deletable = true)
        {            
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Red)
                .WithTitle(title.IsEmptyOrWhiteSpace() ? "🚫" : title)
                .WithDescription(message);
            return deletable
                ? await ctx.RespondDeletableAsync(builder.Build())
                : await ctx.RespondAsync(builder.Build());
        }
    }
}
