using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MonkeyBot.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MonkeyBot.Modules
{
    public class MiscModule : BaseCommandModule
    {
        [Command("FindMessageID")]
        [Description("Gets the message id of a message in the current channel with the provided message text")]
        [RequireGuild]
        [Example("FindMessageID The quick brown fox jumps over the lazy dog")]
        public async Task FindMessageIDAsync(CommandContext ctx, [Description("The content of the message to search for")][RemainingText] string messageContent)
        {
            if (messageContent.IsEmptyOrWhiteSpace())
            {
                _ = await ctx.ErrorAsync("You need to specify the text of the message to search for");
                return;
            }
            const int searchDepth = 100;
            var messages = await ctx.Channel.GetMessagesAsync(searchDepth);
            IEnumerable<DiscordMessage> matches = messages.Where(x => x.Content.StartsWith(messageContent.Trim(), StringComparison.OrdinalIgnoreCase));
            if (matches == null || !matches.Any())
            {
                _ = await ctx.ErrorAsync($"Message not found. Hint: Only the last {searchDepth} messages in this channel are scanned.");
                return;
            }
            else if (matches.Count() > 1)
            {
                _ = await ctx.ErrorAsync($"{matches.Count()} Messages found. Please be more specific");
                return;
            }
            else
            {
                _ = await ctx.OkAsync($"The message Id is: {matches.First().Id}");
            }
        }

        private const string lmgtfyBaseUrl = "https://lmgtfy.com/?q=";

        [Command("lmgtfy")]
        [Description("Generate a 'let me google that for you' link")]
        [Example("lmgtfy Monkey Gamers")]
        public async Task LmgtfyAsync(CommandContext ctx, [RemainingText, Description("Search Text")] string searchText)
        {
            if (searchText.IsEmptyOrWhiteSpace())
            {
                _ = await ctx.RespondAsync("You have to provide a search text");
                return;
            }
            string url = lmgtfyBaseUrl + HttpUtility.UrlEncode(searchText);
            var builder = new DiscordEmbedBuilder()
                .WithTitle("Let me google that for you")
                .WithThumbnail("https://lmgtfy.com/assets/SERP/lmgtfy_logo.png")                
                .WithDescription($"{ctx.Member.Nickname ?? ctx.Member.Username} wants to point you to a magic place filled with answers. [Just click this link]({url})")
                .WithUrl(url);
            _ = await ctx.RespondAsync(builder.Build());
        }
    }
}