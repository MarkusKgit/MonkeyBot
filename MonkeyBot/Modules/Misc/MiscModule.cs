using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MonkeyBot.Common;
using MonkeyBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MonkeyBot.Modules
{
    public class MiscModule : BaseCommandModule
    {
        //private readonly MonkeyDBContext dbContext;
        private readonly IGuildService guildService;

        public MiscModule(IGuildService guildService)
        {
            this.guildService = guildService;
        }

        [Command("Rules")]
        [Description("The bot replies with the server rules")]
        [RequireGuild]
        public async Task ListRulesAsync(CommandContext ctx)
        {
            List<string> rules = (await guildService.GetOrCreateConfigAsync(ctx.Guild.Id).ConfigureAwait(false)).Rules;
            if (rules == null || rules.Count < 1)
            {
                _ = await ctx.RespondAsync("No rules set!").ConfigureAwait(false);
                return;
            }
            var builder = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.DarkGreen)
                .WithTitle($"Rules of {ctx.Guild.Name}")
                .WithDescription(string.Join(Environment.NewLine, rules));

            _ = await ctx.RespondDeletableAsync(embed: builder.Build()).ConfigureAwait(false);

        }

        [Command("FindMessageID")]
        [Description("Gets the message id of a message in the current channel with the provided message text")]
        [RequireGuild]
        public async Task FindMessageIDAsync(CommandContext ctx, [Description("The content of the message to search for")][RemainingText] string messageContent)
        {
            if (messageContent.IsEmptyOrWhiteSpace())
            {
                _ = await ctx.ErrorAsync("You need to specify the text of the message to search for").ConfigureAwait(false);
                return;
            }
            const int searchDepth = 100;
            var messages = await ctx.Channel.GetMessagesAsync(searchDepth).ConfigureAwait(false);
            IEnumerable<DiscordMessage> matches = messages.Where(x => x.Content.StartsWith(messageContent.Trim(), StringComparison.OrdinalIgnoreCase));
            if (matches == null || !matches.Any())
            {
                _ = await ctx.ErrorAsync($"Message not found. Hint: Only the last {searchDepth} messages in this channel are scanned.").ConfigureAwait(false);
                return;
            }
            else if (matches.Count() > 1)
            {
                _ = await ctx.ErrorAsync($"{matches.Count()} Messages found. Please be more specific").ConfigureAwait(false);
                return;
            }
            else
            {
                _ = await ctx.OkAsync($"The message Id is: {matches.First().Id}").ConfigureAwait(false);
            }
        }

        private const string lmgtfyBaseUrl = "https://lmgtfy.com/?q=";

        [Command("lmgtfy")]
        [Description("Generate a 'let me google that for you' link")]
        [Example("!lmgtfy Monkey Gamers")]
        public async Task LmgtfyAsync(CommandContext ctx, [RemainingText, Description("Search Text")] string searchText)
        {
            if (searchText.IsEmptyOrWhiteSpace())
            {
                _ = await ctx.RespondAsync("You have to provide a search text").ConfigureAwait(false);
                return;
            }
            string url = lmgtfyBaseUrl + HttpUtility.UrlEncode(searchText);
            _ = await ctx.RespondAsync(url).ConfigureAwait(false);
        }
    }
}