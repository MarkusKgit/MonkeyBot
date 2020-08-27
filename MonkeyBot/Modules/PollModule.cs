using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    /// <summary>
    /// Provides a simple voting system
    /// </summary>
    [Description("Simple poll")]
    [RequireGuild]
    [MinPermissions(AccessLevel.User)]
    public class PollModule : InteractiveBase
    {
        private static readonly TimeSpan pollDuration = TimeSpan.FromHours(1);

        public PollModule()
        {
        }

        [Command("Poll")]
        [Aliases("Vote")]
        [Priority(1)]
        [Description("Starts a new poll with the specified question and automatically adds reactions")]
        [Example("!poll \"Is MonkeyBot awesome?\"")]
        [RequireBotPermission(ChannelPermission.AddReactions | ChannelPermission.ManageMessages)]
        public async Task StartPollAsync([Summary("The question")][RemainingText] string question)
        {
            question = question.Trim('\"');
            if (question.IsEmpty())
            {
                _ = await ctx.RespondAsync("Please enter a question").ConfigureAwait(false);
                return;
            }
            var poll = new Poll
            {
                GuildId = Context.Guild.Id,
                ChannelId = Context.Channel.Id,
                Question = question,
                Answers = new List<PollAnswer>
                {
                    new PollAnswer("Yes", new Emoji("👍")),
                    new PollAnswer("No", new Emoji("👎")),
                    new PollAnswer("Don't care", new Emoji("🤷"))
                }
            };
            _ = await InlineReactionReplyAsync(GeneratePoll(poll), false).ConfigureAwait(false);
        }

        [Command("Poll")]
        [Aliases("Vote")]
        [Priority(2)]
        [Description("Starts a new poll with the specified question and the list answers and automatically adds reactions")]
        [Example("!poll \"How cool is MonkeyBot?\" \"supercool\" \"over 9000\" \"bruh...\"")]
        [RequireBotPermission(ChannelPermission.AddReactions | ChannelPermission.ManageMessages)]
        public async Task StartPollAsync([Summary("The question")] string question, [Summary("The list of answers")] params string[] answers)
        {
            if (answers == null || answers.Length <= 0)
            {
                await StartPollAsync(question).ConfigureAwait(false);
                return;
            }
            if (answers.Length < 2)
            {
                _ = await ctx.RespondAsync("Please provide at least 2 answers").ConfigureAwait(false);
                return;
            }
            if (answers.Length > 7)
            {
                _ = await ctx.RespondAsync("Please provide a maximum of 7 answers").ConfigureAwait(false);
                return;
            }
            question = question.Trim('\"');
            if (question.IsEmptyOrWhiteSpace())
            {
                _ = await ctx.RespondAsync("Please enter a question").ConfigureAwait(false);
                return;
            }

            var poll = new Poll
            {
                GuildId = Context.Guild.Id,
                ChannelId = Context.Channel.Id,
                Question = question,
                Answers = answers.Select((ans, i) => new PollAnswer(ans, new Emoji(MonkeyHelpers.GetUnicodeRegionalLetter(i)))).ToList()
            };
            _ = await InlineReactionReplyAsync(GeneratePoll(poll), false).ConfigureAwait(false);
        }

        private static ReactionCallbackData GeneratePoll(Poll poll)
        {
            string answers = string.Join(Environment.NewLine, poll.Answers.Select(x => $"{x.AnswerEmoji} {x.Answer}"));

            var embedBuilder = new DiscordEmbedBuilder()
                .WithTitle($"New Poll: {poll.Question}")
                .WithColor(new Color(20, 20, 20))
                .WithDescription(
                    "- Pick an option by clicking on the corresponding Emoji" + Environment.NewLine
                    + "- Only your first pick counts!" + Environment.NewLine
                    + $"- You have {pollDuration.Humanize()} to cast your vote"
                    )
                .AddField("Pick one", answers);

            var rcbd = new ReactionCallbackData("", embedBuilder.Build(), false, true, true, pollDuration, async c => await PollEndedAsync(c, poll).ConfigureAwait(false));
            foreach (Emoji answerEmoji in poll.Answers.Select(x => x.AnswerEmoji))
            {
                _ = rcbd.WithCallback(answerEmoji, (c, r) => AddVoteCount(r, poll));
            }
            return rcbd;
        }

        private static Task AddVoteCount(SocketReaction reaction, Poll poll)
        {
            PollAnswer answer = poll.Answers.SingleOrDefault(e => e.AnswerEmoji.Equals(reaction.Emote));
            if (answer != null && reaction.User.IsSpecified)
            {
                _ = poll.ReactionUsers.AddOrUpdate(
                        answer,
                        new List<IUser> { reaction.User.Value },
                        (_, list) =>
                          {
                              list.Add(reaction.User.Value);
                              return list;
                          }
                );
            }
            return Task.CompletedTask;
        }

        private static async Task PollEndedAsync(SocketCommandContext context, Poll poll)
        {
            if (poll == null)
            {
                return;
            }
            IEnumerable<string> answerCounts = poll.Answers.Select(answer => $"{answer.Answer}: { poll.ReactionUsers.FirstOrDefault(x => x.Key.Equals(answer)).Value?.Count.ToString() ?? "0"}");
            List<IUser> participants = poll.ReactionUsers.Select(x => x.Value).SelectMany(x => x).ToList();
            string participantsString = "-";
            if (participants != null && participants.Count > 0)
            {
                participantsString = string.Join(", ", participants?.Select(x => x.Mention));
            }
            var embedBuilder = new DiscordEmbedBuilder()
                .WithTitle($"Poll ended: {poll.Question}")
                .WithColor(new Color(20, 20, 20))
                .AddField("Results", string.Join(Environment.NewLine, answerCounts))
                .AddField("Voters", participantsString);

            _ = await context.Channel.SendMessageAsync("", embed: embedBuilder.Build()).ConfigureAwait(false);
        }
    }
}