using System;
using Discord;
using Discord.Commands;
using MonkeyBot.Preconditions;
using MonkeyBot.Common;
using System.Threading.Tasks;
using System.Text;

namespace MonkeyBot.Modules
{
    /// <summary>
    /// Provides a simple voting system
    /// </summary>
    [MinPermissions(AccessLevel.User)]
    [Name("Simple poll")]
    public class PollModule : ModuleBase
    {
        [Command("Poll")]
        [Alias("Vote")]
        [Remarks("Starts a new poll with the specified question and automatically adds reactions")]
        [MinPermissions(AccessLevel.BotOwner)]
        [RequireContext(ContextType.Guild)]
        public async Task Vote([Summary("The question")][Remainder] string question)
        {
            question = question.Trim('\"');
            if (string.IsNullOrEmpty(question))
            {
                await ReplyAsync("Please enter a question");
                return;
            }            
            if (!question.EndsWith("?"))
                question += "?";
            var msg = await Context.Channel.SendMessageAsync(question);
            if (msg != null)
            {
                await msg.AddReactionAsync(Emote.Parse("<:yes:>"));
                await msg.AddReactionAsync(Emote.Parse("<:no:>"));
                await msg.AddReactionAsync(Emote.Parse("<:shrug:>"));
            }
        }

        [Command("Poll")]
        [Alias("Vote")]
        [Remarks("Starts a new poll with the specified question and answers and automatically adds reactions")]
        [MinPermissions(AccessLevel.BotOwner)]
        [RequireContext(ContextType.Guild)]
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
            if (!question.EndsWith("?"))
                question += "?";
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(question);
            int indexOfA = (int)char.ToUpper('A');
            for (int i = 0; i < answers.Length; i++)
            {
                char reactionLetter = (char)(indexOfA + i);
                builder.AppendLine($"{reactionLetter}: {answers[i]}");
            }
            var msg = await Context.Channel.SendMessageAsync(builder.ToString());
            if (msg != null)
            {
                for (int i = 0; i < answers.Length; i++)
                {
                    char reactionLetter = (char)(indexOfA + i);
                    await msg.AddReactionAsync(Emote.Parse($"<:{reactionLetter}:>"));
                }                
            }
        }
    }
}
