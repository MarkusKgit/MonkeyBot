using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using Fclp.Internals.Extensions;
using Humanizer;
using Microsoft.EntityFrameworkCore.Internal;
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
        private static readonly DiscordEmoji okEmoji = DiscordEmoji.FromUnicode("👍");
        private const int timeOutSeconds = 60;
        private readonly TimeSpan timeOut = TimeSpan.FromSeconds(timeOutSeconds);

        private readonly string introText =
            $"I will now guide you through the creation of the poll with a set of instructions.\n" +
            $"You have {timeOutSeconds} seconds to answer each question \n" +
            $"Above you can see a preview of the poll that will get created \n\n";

        private const string firstInstruction = "**1. Type a poll question**";
        private const string secondInstruction = "**2. When should the poll end? Valid examples are:** \n" +
            "*17:00* \n" +
            "*Tomorrow 17:00* \n" +
            "*Monday 17:00* \n";
        private const string thirdInstruction = "**3. Now enter at least 2 poll answers by sending a message for each answer. Once you are done click \"👍\" to start the poll**";

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

            var questionReponse = await interactivity.WaitForMessageAsync(msg => msg.Author == ctx.Member && msg.ChannelId == ctx.Channel.Id, timeOut).ConfigureAwait(false);
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

            pollEmbed.WithTitle($"**Poll: {pollQuestion}**");
            pollMessage = await pollMessage.ModifyAsync(embed: pollEmbed.Build()).ConfigureAwait(false);

            await ctx.Channel.DeleteMessageAsync(questionReponse.Result).ConfigureAwait(false);

            setupEmbed.WithDescription(introText + secondInstruction);
            await setupMessage.ModifyAsync(embed: setupEmbed.Build()).ConfigureAwait(false);

            var timeResponse = await interactivity.WaitForMessageAsync(msg => msg.Author == ctx.Member && msg.ChannelId == ctx.Channel.Id, timeOut).ConfigureAwait(false);
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
            if (endTime < DateTime.Now)
            {
                _ = await ctx.ErrorAsync("The provided time is in the past. Please start over", "Invalid Time").ConfigureAwait(false);
                await ctx.Channel.DeleteMessagesAsync(new[] { setupMessage, pollMessage }).ConfigureAwait(false);
                return;
            }

            pollEmbed.WithFooter($"Poll will end on {endTime:dd.MM.yyyy} at {endTime:HH:mm \"UTC\"zz}");
            pollMessage = await pollMessage.ModifyAsync(embed: pollEmbed.Build()).ConfigureAwait(false);

            await ctx.Channel.DeleteMessageAsync(timeResponse.Result).ConfigureAwait(false);

            setupEmbed.WithDescription(introText + thirdInstruction);
            await setupMessage.ModifyAsync(embed: setupEmbed.Build()).ConfigureAwait(false);
            await setupMessage.CreateReactionAsync(okEmoji).ConfigureAwait(false);

            var pollAnswers = new Dictionary<DiscordEmoji, string>();
            while (true)
            {
                var addAnswerTask = interactivity.WaitForMessageAsync(msg => msg.Author == ctx.Member && msg.ChannelId == ctx.Channel.Id, timeOut);
                var okTask = interactivity.WaitForReactionAsync(r => r.User == ctx.Member && r.Emoji == okEmoji, timeOut);
                var result = await Task.WhenAny(addAnswerTask, okTask).ConfigureAwait(false);

                if (result == addAnswerTask)
                {
                    var pollOptionResponse = addAnswerTask.Result;
                    if (pollOptionResponse.TimedOut)
                    {
                        if (pollAnswers.Count >= 2)
                        {
                            break;
                        }
                        else
                        {
                            _ = await ctx.ErrorAsync("You didn't provide enough answers in time. Please start over", "Timed out").ConfigureAwait(false);
                            await ctx.Channel.DeleteMessagesAsync(new[] { setupMessage, pollMessage }).ConfigureAwait(false);
                            return;
                        }
                    }
                    pollAnswers.Add(DiscordEmoji.FromUnicode(MonkeyHelpers.GetUnicodeRegionalLetter(pollAnswers.Count)), pollOptionResponse.Result.Content);
                    await ctx.Channel.DeleteMessageAsync(pollOptionResponse.Result).ConfigureAwait(false);
                    pollEmbed.WithDescription(string.Join("\n", pollAnswers.Select(ans => $"{ans.Key} {ans.Value}")));
                    pollMessage = await pollMessage.ModifyAsync(embed: pollEmbed.Build()).ConfigureAwait(false);
                }
                else
                {
                    if (pollAnswers.Count < 2)
                    {
                        _ = await ctx.ErrorAsync("Not enough answer options! Please add more first!").ConfigureAwait(false);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            await ctx.Channel.DeleteMessageAsync(setupMessage).ConfigureAwait(false);

            foreach (DiscordEmoji pollEmoji in pollAnswers.Keys)
            {
                await pollMessage.CreateReactionAsync(pollEmoji).ConfigureAwait(false);
            }
            await pollMessage.PinAsync().ConfigureAwait(false);

            TimeSpan pollDuration = endTime - DateTime.Now;
            var pollResult = await interactivity.DoPollAsync(pollMessage, pollAnswers.Keys.ToArray(), PollBehaviour.KeepEmojis, pollDuration).ConfigureAwait(false);
            await pollMessage.UnpinAsync().ConfigureAwait(false);
            if (!pollResult.Any(x => x.Total > 0))
            {
                _ = await ctx.RespondAsync($"No one participated in the poll {pollQuestion} :(").ConfigureAwait(false);
                return;
            }
            int totalVotes = pollResult.Sum(r => r.Total);
            var pollResultEmbed = new DiscordEmbedBuilder()
                .WithTitle($"Poll results: {pollQuestion}")
                .WithColor(DiscordColor.Azure)
                .WithDescription($"**{ctx.Member.Mention}{(ctx.Member.Username.EndsWith('s') ? "'" : "'s")} poll ended. Here are the results:**\n\n" +
                string.Join("\n", pollAnswers
                    .Select(ans => new {Answer = ans.Value, Votes = pollResult.Single(r => r.Emoji == ans.Key).Total})
                    .Select(ans => $"**{ans.Answer}**: {"vote".ToQuantity(ans.Votes)} ({100.0 * ans.Votes / totalVotes:F1} %)"))
                );
            _ = await ctx.RespondAsync(embed: pollResultEmbed.Build()).ConfigureAwait(false);
        }
    }
}