using Discord.Commands;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [Name("Info")]
    public class InfoModule : ModuleBase
    {
        [Command("say")]
        [Remarks("The bot replies with the specified message")]
        public async Task SayAsync([Remainder, Summary("The text to echo")] string msg)
        {
            await ReplyAsync(msg);
        }
    }
}