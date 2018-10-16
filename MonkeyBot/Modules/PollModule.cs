using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using dokas.FluentStrings;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
using MonkeyBot.Services.Common.Poll;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public class PollModule : InteractiveBase
    {
        public PollModule()
        {
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
            //await pollService.AddPollAsync(poll);
            //await InlineReactionReplyAsync(GeneratePoll(poll), false);            
        }

        private static ReactionCallbackData GeneratePoll(Poll poll)
        {
            var rcbd = new ReactionCallbackData(poll.Question, null, false, true, TimeSpan.FromMinutes(15));
            foreach (var answerEmoji in poll.Answers)
            {
                rcbd.WithCallback(answerEmoji, (c,r) => AddVoteCount(c, r, poll));                
            }
            return rcbd;
        }

        private static Task AddVoteCount(SocketCommandContext context, SocketReaction reaction, Poll poll)
        {
            int reactionIndex = poll.Answers.Select(x => x.Name).ToList().IndexOf(reaction.Emote.Name);
            if (reactionIndex >= 0)
                poll.ReactionCount.AddOrUpdate(reactionIndex, reaction.UserId, (_, __) => reaction.UserId);
            return Task.CompletedTask;
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
                reactions.Add(new Emoji(MonkeyHelpers.GetUnicodeRegionalLetter(i)));
            }
            Poll poll = new Poll
            {
                GuildId = Context.Guild.Id,
                ChannelId = Context.Channel.Id,
                Question = questionBuilder.ToString(),
                Answers = new List<Emoji>(reactions)
            };
            //await pollService.AddPollAsync(poll);
            //await InlineReactionReplyAsync(GeneratePoll(poll), false);
        }
    }
}