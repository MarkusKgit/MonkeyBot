using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using Fclp.Internals.Extensions;
using Humanizer;
using MonkeyBot.Common;
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
    public class PollModule : BaseCommandModule
    {
        private static InteractivityExtension interactivity;
        private static Chronic.Parser timeParser;

        private static DiscordEmoji okEmoji = DiscordEmoji.FromUnicode("👍");
        private static DiscordEmoji plusEmoji = DiscordEmoji.FromUnicode("➕");

        //private static readonly Dictionary<DiscordEmoji, string> yesNoPollAnswers = new Dictionary<DiscordEmoji, string>()
        //{
        //    {DiscordEmoji.FromUnicode("👍"), "Yes" },
        //    {DiscordEmoji.FromUnicode("👎"), "No" },
        //    {DiscordEmoji.FromUnicode("🤷"), "Don't care" }
        //};

        private const string introText =
            "I will now guide you through the creation of the poll with a set of instructions.\n" +
            "Watch this message for updated steps \n" +
            "Above you can see a preview of the poll that will get created \n\n";            

        private const string firstInstruction = "**1. Type a poll question**";
        private const string secondInstruction = "**2. When should the poll end? Valid examples are:** \n" +
            "*17:00* \n" +
            "*Tomorrow 17:00* \n" +
            "*Monday 17:00* \n";
        private const string thirdInstruction = "**3. By clicking on \"+\" you can add a possible answer to the poll. You will be prompted for the text each time. Once you are done click \"👍\" to start the poll**";

        [Command("Poll")]
        [Aliases("Vote")]
        [Description("Starts a new poll with the specified question and automatically adds reactions")]
        [RequireBotPermissions(Permissions.AddReactions | Permissions.ManageMessages)]
        public async Task StartPollAsync(CommandContext ctx)
        {
            var pollEmbed = new DiscordEmbedBuilder()
                .WithTitle("New poll")
                .WithColor(DiscordColor.Azure)
                .WithDescription("...")
                .WithAuthor(ctx.Member.Username, iconUrl: ctx.Member.AvatarUrl);

            DiscordMessage pollMessage = await ctx.RespondAsync(embed: pollEmbed.Build()).ConfigureAwait(false);

            var setupEmbed = new DiscordEmbedBuilder()
                .WithTitle("Poll Configuration")
                .WithColor(DiscordColor.Gold)
                .WithDescription(introText + firstInstruction)
                .WithAuthor(ctx.Client.CurrentUser.Username, iconUrl: ctx.Client.CurrentUser.AvatarUrl);

            DiscordMessage setupMessage = await ctx.RespondAsync(embed: setupEmbed.Build()).ConfigureAwait(false);

            interactivity ??= ctx.Client.GetInteractivity();

            var questionReponse = await interactivity.WaitForMessageAsync(msg =>
            {
                return msg.Author == ctx.Member && msg.ChannelId == ctx.Channel.Id;
                }, TimeSpan.FromSeconds(60)).ConfigureAwait(false);
            if (questionReponse.TimedOut)
            {
                _ = await ctx.ErrorAsync("You didn't respond in time. Please start over", "Timed out").ConfigureAwait(false);
                await ctx.Channel.DeleteMessagesAsync(new[] { setupMessage, pollMessage }).ConfigureAwait(false);
                return;
            }
            string pollQuestion = questionReponse.Result.Content.Trim();
            if (pollQuestion.IsNullOrEmpty())
            {
                _ = await ctx.ErrorAsync("You didn't provide a proper poll question. Please start over", "Empty question").ConfigureAwait(false);
                await ctx.Channel.DeleteMessagesAsync(new[] { setupMessage, pollMessage }).ConfigureAwait(false);
                return;
            }

            pollEmbed.WithTitle($"**{pollQuestion}**");
            pollMessage = await pollMessage.ModifyAsync(embed: pollEmbed.Build()).ConfigureAwait(false);

            setupEmbed.WithDescription(introText + secondInstruction);
            setupMessage = await setupMessage.ModifyAsync(embed: setupEmbed.Build()).ConfigureAwait(false);
            
            var timeResponse = await interactivity.WaitForMessageAsync(msg =>
            {
                return msg.Author == ctx.Member && msg.ChannelId == ctx.Channel.Id;
            }, TimeSpan.FromSeconds(60)).ConfigureAwait(false);
            if (timeResponse.TimedOut)
            {
                _ = await ctx.ErrorAsync("You didn't respond in time. Please start over", "Timed out").ConfigureAwait(false);
                await ctx.Channel.DeleteMessagesAsync(new[] { setupMessage, pollMessage }).ConfigureAwait(false);
                return;
            }
            timeParser ??= new Chronic.Parser(new Chronic.Options() { Context = Chronic.Pointer.Type.Future, EndianPrecedence = Chronic.EndianPrecedence.Little, FirstDayOfWeek = DayOfWeek.Monday });
            Chronic.Span parsedTime = timeParser.Parse(timeResponse.Result.Content);
            if (parsedTime == null || parsedTime.ToTime() == null)
            {
                _ = await ctx.ErrorAsync("I couldn't understand this Date/Time. Please start over", "Invalid Time").ConfigureAwait(false);
                await ctx.Channel.DeleteMessagesAsync(new[] { setupMessage, pollMessage }).ConfigureAwait(false);
                return;
            }
            DateTime endTime = parsedTime.ToTime();

            pollEmbed.WithFooter($"Vote will end {endTime.ToUniversalTime()} UTC");
            pollMessage = await pollMessage.ModifyAsync(embed: pollEmbed.Build()).ConfigureAwait(false);

            setupEmbed.WithDescription(introText + thirdInstruction);
            setupMessage = await setupMessage.ModifyAsync(embed: setupEmbed.Build()).ConfigureAwait(false);
            await setupMessage.CreateReactionAsync(plusEmoji).ConfigureAwait(false);
            await setupMessage.CreateReactionAsync(okEmoji).ConfigureAwait(false);

            bool keepAdding = true;
            List<string> pollAnswers = new List<string>();
            while (keepAdding)
            {
                var reaction = await interactivity.WaitForReactionAsync(r => r.User == ctx.Member && r.Emoji == okEmoji || r.Emoji == plusEmoji).ConfigureAwait(false);
                if (reaction.TimedOut || reaction.Result.Emoji == okEmoji)
                {
                    keepAdding = false;
                }
                else
                {
                    await setupMessage.DeleteReactionAsync(plusEmoji, ctx.Member).ConfigureAwait(false);
                }
            }
        }

        //[Command("Poll")]
        //[Aliases("Vote")]
        //[Priority(1)]
        //[Description("Starts a new poll with the specified question and automatically adds reactions")]
        //[Example("!poll \"Is MonkeyBot awesome?\"")]
        //[RequireBotPermissions(Permissions.AddReactions | Permissions.ManageMessages)]
        //public async Task StartPollAsync(CommandContext ctx, [Description("The question")][RemainingText] string question)
        //{
        //    question = question.Trim('\"');
        //    if (question.IsEmpty())
        //    {
        //        _ = await ctx.RespondAsync("Please enter a question").ConfigureAwait(false);
        //        return;
        //    }

        //    Dictionary<DiscordEmoji, string> possibleAnswers = yesNoPollAnswers;

        //    await DoPollInternalAsync(ctx, question, possibleAnswers).ConfigureAwait(false);
        //}        

        //[Command("Poll")]
        //[Aliases("Vote")]
        //[Priority(2)]
        //[Description("Starts a new poll with the specified question and the list answers and automatically adds reactions")]
        //[Example("!poll \"How cool is MonkeyBot?\" \"supercool\" \"over 9000\" \"bruh...\"")]
        //[RequireBotPermissions(Permissions.AddReactions | Permissions.ManageMessages)]
        //public async Task StartPollAsync(CommandContext ctx, [Description("The question")] string question, [Description("The list of answers")] params string[] answers)
        //{
        //    if (answers == null || answers.Length <= 2)
        //    {
        //        _ = await ctx.ErrorAsync("Please provide at least 2 answers").ConfigureAwait(false);
        //        return;
        //    }
        //    if (answers.Length > 7)
        //    {
        //        _ = await ctx.ErrorAsync("Please provide a maximum of 7 answers").ConfigureAwait(false);
        //        return;
        //    }
        //    question = question.Trim('\"');
        //    if (question.IsEmptyOrWhiteSpace())
        //    {
        //        _ = await ctx.ErrorAsync("Please enter a question").ConfigureAwait(false);
        //        return;
        //    }

        //    var possibleAnswers = answers.Select((ans, i) => (DiscordEmoji.FromUnicode(MonkeyHelpers.GetUnicodeRegionalLetter(i)), ans)).ToDictionary(x => x.Item1, x => x.ans);

        //    await DoPollInternalAsync(ctx, question, possibleAnswers).ConfigureAwait(false);
        //}

        //private async Task DoPollInternalAsync(CommandContext ctx, string question, Dictionary<DiscordEmoji, string> possibleAnswers)
        //{
        //    var embedBuilder = new DiscordEmbedBuilder()
        //                    .WithTitle($"New Poll: {question}")
        //                    .WithColor(new DiscordColor(20, 20, 20))
        //                    .WithDescription(
        //                        "- Pick an option by clicking on the corresponding Emoji" + Environment.NewLine
        //                        + "- You can only select one option" + Environment.NewLine
        //                        + $"- You have {pollDuration.Humanize()} to cast your vote"
        //                        )
        //                    .AddField("Pick one", string.Join("\n", possibleAnswers.Select(x => $"{x.Key}: {x.Value}")));

        //    DiscordMessage m = await ctx.RespondAsync(embed: embedBuilder.Build()).ConfigureAwait(false);

        //    interactivity ??= ctx.Client.GetInteractivity();

        //    var pollResult = await interactivity.DoPollAsync(m, possibleAnswers.Keys.ToArray(), PollBehaviour.DeleteEmojis, pollDuration).ConfigureAwait(false);

        //    IEnumerable<string> answerCounts = possibleAnswers.Select(x => $"{x.Value}: {pollResult.SingleOrDefault(pr => pr.Emoji == x.Key)?.Total ?? 0}");

        //    IEnumerable<string> participants = pollResult.SelectMany(x => x.Voted).Select(x => x.Mention).Distinct();

        //    if (!participants.Any())
        //    {
        //        _ = await ctx.RespondAsync($"Not a single person voted on \"{question}\"").ConfigureAwait(false);
        //        return;
        //    }
        //    embedBuilder = new DiscordEmbedBuilder()
        //        .WithTitle($"Poll \"{question}\" ended")
        //        .WithColor(new DiscordColor(20, 20, 20))
        //        .AddField("Results", string.Join('\n', answerCounts))
        //        .AddField("Voters", string.Join(", ", participants));

        //    _ = await ctx.RespondAsync(embed: embedBuilder.Build()).ConfigureAwait(false);
        //}

    }
}