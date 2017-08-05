using Discord;
using Discord.Commands;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MonkeyBot.Services;
using MonkeyBot.Services.Common.Poll;

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
        private IPollService pollService;

        public PollModule(IServiceProvider provider)
        {
            pollService = provider.GetService<IPollService>();           
        }
        
        [Command("Poll")]
        [Alias("Vote")]
        [Priority(1)]
        [Remarks("Starts a new poll with the specified question and automatically adds reactions")]
        public async Task StartPollAsync([Summary("The question")][Remainder] string question)
        {
            question = question.Trim('\"');
            if (string.IsNullOrEmpty(question))
            {
                await ReplyAsync("Please enter a question");
                return;
            }
            List<Emoji> reactions = new List<Emoji>() { new Emoji("👍"), new Emoji("👎"), new Emoji("🤷") };
            Poll poll = new Poll()
            {
                GuildId = Context.Guild.Id,
                ChannelId = Context.Channel.Id,
                Question = question,
                Answers = new List<Emoji>(reactions)
            };
            await pollService.AddPollAsync(poll);            
        }
        
        [Command("Poll")]
        [Alias("Vote")]
        [Priority(2)]
        [Remarks("Starts a new poll with the specified question and the list answers and automatically adds reactions")]
        public async Task StartPollAsync([Summary("The question")] string question, [Summary("The list of answers")] params string[] answers)
        {
            if (answers == null || answers.Length <= 0)
            {
                await StartPollAsync(question);
                return;
            }
            if (answers.Length > 7)
            {
                await ReplyAsync("Please provide a maximum of 7 answers");
                return;
            }
            question = question.Trim('\"');
            if (string.IsNullOrEmpty(question))
            {
                await ReplyAsync("Please enter a question");
                return;
            }

            StringBuilder questionBuilder = new StringBuilder();
            List<Emoji> reactions = new List<Emoji>();
            questionBuilder.AppendLine(question);
            for (int i = 0; i < answers.Length; i++)
            {
                char reactionLetter = (char)('A' + i);
                questionBuilder.AppendLine($"{reactionLetter}: {answers[i].Trim()}");
                reactions.Add(new Emoji(GetUnicodeRegionalLetter(i)));
            }
            Poll poll = new Poll()
            {
                GuildId = Context.Guild.Id,
                ChannelId = Context.Channel.Id,
                Question = questionBuilder.ToString(),
                Answers = new List<Emoji>(reactions)
            };
            await pollService.AddPollAsync(poll);
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