using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;
namespace MonkeyBot.Modules
{
    public class InfoModule : ModuleBase
    {        
        [Command("say"), Summary("Echos a message.")]
        public async Task Say([Remainder, Summary("The text to echo")] string echo)
        {                        
            await ReplyAsync(echo);
        }

        [Command("whoami"), Summary("Tells you who you are")]
        public async Task WhoAmI()
        {
            await ReplyAsync(Context.User.ToString());
        }
    }
}
