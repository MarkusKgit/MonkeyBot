using Discord;
using Discord.Commands;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    /// <summary>
    /// Provides a simple voting system
    /// </summary>
    [Name("Simple poll")]
    [RequireContext(ContextType.Guild)]
    [MinPermissions(AccessLevel.User)]
    public class PollModule : ModuleBase
    {
        [Command("Poll")]
        [Alias("Vote")]
        [Priority(1)]
        [Remarks("Starts a new poll with the specified question and automatically adds reactions")]
        public async Task Vote([Summary("The question")][Remainder] string question)
        {
            question = question.Trim('\"');
            if (string.IsNullOrEmpty(question))
            {
                await ReplyAsync("Please enter a question");
                return;
            }

            var msg = await Context.Channel.SendMessageAsync(question);
            if (msg != null)
            {
                await msg.AddReactionAsync(new Emoji("👍"));
                await msg.AddReactionAsync(new Emoji("👎"));
                await msg.AddReactionAsync(new Emoji("🤷"));
            }
        }

        [Command("Poll")]
        [Alias("Vote")]
        [Priority(2)]
        [Remarks("Starts a new poll with the specified question and the list answers and automatically adds reactions")]
        public async Task Vote([Summary("The question")] string question, [Summary("The list of answers")] params string[] answers)
        {
            if (answers == null || answers.Length <= 0)
            {
                await Vote(question);
                return;
            }
            question = question.Trim('\"');
            if (string.IsNullOrEmpty(question))
            {
                await ReplyAsync("Please enter a question");
                return;
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(question);
            for (int i = 0; i < answers.Length; i++)
            {
                char reactionLetter = (char)('A' + i);
                builder.AppendLine($"{reactionLetter}: {answers[i]}");
            }
            var msg = await Context.Channel.SendMessageAsync(builder.ToString());
            if (msg != null)
            {
                for (int i = 0; i < answers.Length; i++)
                {
                    await msg.AddReactionAsync(new Emoji(GetUnicodeRegionalLetter(i)));
                }
            }
        }

        private string GetUnicodeRegionalLetter(int index)
        {
            if (index == 0)
                return "🇦";
            else if (index == 1)
                return "🇧";
            else if (index == 2)
                return "🇨";
            else if (index == 3)
                return "🇩";
            else if (index == 4)
                return "🇪";
            else if (index == 5)
                return "🇫";
            else if (index == 6)
                return "🇬";
            else
                return "";
        }
    }
}