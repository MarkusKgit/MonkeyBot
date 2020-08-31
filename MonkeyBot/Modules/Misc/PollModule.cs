using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
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
        private static readonly TimeSpan pollDuration = TimeSpan.FromHours(1);

        private static InteractivityExtension interactivity;

        private static readonly Dictionary<DiscordEmoji, string> yesNoPollAnswers = new Dictionary<DiscordEmoji, string>()
        {
            {DiscordEmoji.FromUnicode("👍"), "Yes" },
            {DiscordEmoji.FromUnicode("👎"), "No" },
            {DiscordEmoji.FromUnicode("🤷"), "Don't care" }
        };

        [Command("Poll")]
        [Aliases("Vote")]
        [Priority(1)]
        [Description("Starts a new poll with the specified question and automatically adds reactions")]
        [Example("!poll \"Is MonkeyBot awesome?\"")]
        [RequireBotPermissions(Permissions.AddReactions | Permissions.ManageMessages)]
        public async Task StartPollAsync(CommandContext ctx, [Description("The question")][RemainingText] string question)
        {
            question = question.Trim('\"');
            if (question.IsEmpty())
            {
                _ = await ctx.RespondAsync("Please enter a question").ConfigureAwait(false);
                return;
            }

            Dictionary<DiscordEmoji, string> possibleAnswers = yesNoPollAnswers;

            await DoPollInternalAsync(ctx, question, possibleAnswers).ConfigureAwait(false);
        }        

        [Command("Poll")]
        [Aliases("Vote")]
        [Priority(2)]
        [Description("Starts a new poll with the specified question and the list answers and automatically adds reactions")]
        [Example("!poll \"How cool is MonkeyBot?\" \"supercool\" \"over 9000\" \"bruh...\"")]
        [RequireBotPermissions(Permissions.AddReactions | Permissions.ManageMessages)]
        public async Task StartPollAsync(CommandContext ctx, [Description("The question")] string question, [Description("The list of answers")] params string[] answers)
        {
            if (answers == null || answers.Length <= 2)
            {
                _ = await ctx.ErrorAsync("Please provide at least 2 answers").ConfigureAwait(false);
                return;
            }
            if (answers.Length > 7)
            {
                _ = await ctx.ErrorAsync("Please provide a maximum of 7 answers").ConfigureAwait(false);
                return;
            }
            question = question.Trim('\"');
            if (question.IsEmptyOrWhiteSpace())
            {
                _ = await ctx.ErrorAsync("Please enter a question").ConfigureAwait(false);
                return;
            }

            var possibleAnswers = answers.Select((ans, i) => (DiscordEmoji.FromUnicode(MonkeyHelpers.GetUnicodeRegionalLetter(i)), ans)).ToDictionary(x => x.Item1, x => x.ans);

            await DoPollInternalAsync(ctx, question, possibleAnswers).ConfigureAwait(false);
        }

        private async Task DoPollInternalAsync(CommandContext ctx, string question, Dictionary<DiscordEmoji, string> possibleAnswers)
        {
            var embedBuilder = new DiscordEmbedBuilder()
                            .WithTitle($"New Poll: {question}")
                            .WithColor(new DiscordColor(20, 20, 20))
                            .WithDescription(
                                "- Pick an option by clicking on the corresponding Emoji" + Environment.NewLine
                                + "- You can only select one option" + Environment.NewLine
                                + $"- You have {pollDuration.Humanize()} to cast your vote"
                                )
                            .AddField("Pick one", string.Join("\n", possibleAnswers.Select(x => $"{x.Key}: {x.Value}")));

            DiscordMessage m = await ctx.RespondAsync(embed: embedBuilder.Build()).ConfigureAwait(false);

            interactivity ??= ctx.Client.GetInteractivity();

            var pollResult = await interactivity.DoPollAsync(m, possibleAnswers.Keys.ToArray(), PollBehaviour.DeleteEmojis, pollDuration).ConfigureAwait(false);

            IEnumerable<string> answerCounts = possibleAnswers.Select(x => $"{x.Value}: {pollResult.SingleOrDefault(pr => pr.Emoji == x.Key)?.Total ?? 0}");

            IEnumerable<string> participants = pollResult.SelectMany(x => x.Voted).Select(x => x.Mention).Distinct();

            if (!participants.Any())
            {
                _ = await ctx.RespondAsync($"Not a single person voted on \"{question}\"").ConfigureAwait(false);
                return;
            }
            embedBuilder = new DiscordEmbedBuilder()
                .WithTitle($"Poll \"{question}\" ended")
                .WithColor(new DiscordColor(20, 20, 20))
                .AddField("Results", string.Join('\n', answerCounts))
                .AddField("Voters", string.Join(", ", participants));

            _ = await ctx.RespondAsync(embed: embedBuilder.Build()).ConfigureAwait(false);
        }

    }
}