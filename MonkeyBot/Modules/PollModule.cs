using Discord;
using Discord.Commands;
using dokas.FluentStrings;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
using MonkeyBot.Services.Common.Poll;
using System.Collections.Generic;
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
        private readonly IPollService pollService;

        public PollModule(IPollService pollService)
        {
            this.pollService = pollService;
        }

        [Command("Poll")]
        [Alias("Vote")]
        [Priority(1)]
        [Remarks("Starts a new poll with the specified question and automatically adds reactions")]
        [Example("!poll \"Is MonkeyBot awesome?\"")]
        [RequireBotPermission(ChannelPermission.AddReactions | ChannelPermission.ManageMessages)]
        public async Task StartPollAsync([Summary("The question")][Remainder] string question)
        {
            question = question.Trim('\"');
            if (question.IsEmpty())
            {
                await ReplyAsync("Please enter a question");
                return;
            }
            List<Emoji> reactions = new List<Emoji> { new Emoji("👍"), new Emoji("👎"), new Emoji("🤷") };
            Poll poll = new Poll
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
        [Example("!poll \"How cool is MonkeyBot?\" \"supercool\" \"over 9000\" \"bruh...\"")]
        [RequireBotPermission(ChannelPermission.AddReactions | ChannelPermission.ManageMessages)]
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
            if (question.IsEmpty())
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
            Poll poll = new Poll
            {
                GuildId = Context.Guild.Id,
                ChannelId = Context.Channel.Id,
                Question = questionBuilder.ToString(),
                Answers = new List<Emoji>(reactions)
            };
            await pollService.AddPollAsync(poll);
        }

        private static string GetUnicodeRegionalLetter(int index)
        {
            switch (index)
            {
                case 0:
                    return "🇦";
                case 1:
                    return "🇧";
                case 2:
                    return "🇨";
                case 3:
                    return "🇩";
                case 4:
                    return "🇪";
                case 5:
                    return "🇫";
                case 6:
                    return "🇬";
                default:
                    return "";
            }
        }
    }
}