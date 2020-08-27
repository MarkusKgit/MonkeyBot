using Discord.Commands;
using MonkeyBot.Common;
using System;
using System.Threading.Tasks;
using System.Web;

namespace MonkeyBot.Modules
{
    [Description("Misc")]
    public class MiscModule : MonkeyModuleBase
    {
        private const string lmgtfyBaseUrl = "https://lmgtfy.com/?q=";

        [Command("lmgtfy")]
        [Description("Generate a 'let me google that for you' link")]
        [Example("!lmgtfy Monkey Gamers")]
        public async Task LmgtfyAsync([RemainingText] string searchText)
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