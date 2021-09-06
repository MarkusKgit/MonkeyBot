using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using MonkeyBot.Common;
using MonkeyBot.Models;
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
    public class PollModule : BaseCommandModule
    {
        private readonly IPollService _pollService;

        private static InteractivityExtension _interactivity;
        private static Chronic.Parser _timeParser;

        private static readonly DiscordEmoji _okEmoji = DiscordEmoji.FromUnicode("👍");        
        private static readonly TimeSpan _timeOut = TimeSpan.FromSeconds(60);

        private static readonly string introText =
            $"I will now guide you through the creation of the poll with a set of instructions.\n" +
            $"You have {_timeOut.TotalSeconds} seconds to answer each question \n" +
            $"Above you can see a preview of the poll that will get created \n\n";

        private const string firstInstruction = "**1. Type a poll question**";
        private const string secondInstruction = "**2. When should the poll end? Valid examples are:** \n" +
            "*17:00* \n" +
            "*Tomorrow 17:00* \n" +
            "*Monday 17:00* \n";
        private const string thirdInstruction = "**3. Now enter at least 2 poll answers by sending a message for each answer. Once you are done click \"👍\" to start the poll**";

        public PollModule(IPollService pollService)
        {
            _pollService = pollService;
        }

        //TODO: Convert to buttons instead of reactions

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

            DiscordMessage pollMessage = await ctx.RespondAsync(embed: pollEmbed.Build());

            var setupEmbed = new DiscordEmbedBuilder()
                .WithTitle("Poll Configuration")
                .WithColor(DiscordColor.Gold)
                .WithDescription(introText + firstInstruction)
                .WithAuthor(ctx.Client.CurrentUser.Username, iconUrl: ctx.Client.CurrentUser.AvatarUrl);

            DiscordMessage setupMessage = await ctx.RespondAsync(embed: setupEmbed.Build());

            _interactivity ??= ctx.Client.GetInteractivity();

            var questionReponse = await _interactivity.WaitForMessageAsync(msg => msg.Author == ctx.Member && msg.ChannelId == ctx.Channel.Id, _timeOut);
            if (questionReponse.TimedOut)
            {
                await ctx.ErrorAsync("You didn't respond in time. Please start over", "Timed out");
                await ctx.Channel.DeleteMessagesAsync(new[] { setupMessage, pollMessage });
                return;
            }
            string pollQuestion = questionReponse.Result.Content.Trim();
            if (pollQuestion.IsEmptyOrWhiteSpace())
            {
                await ctx.ErrorAsync("You didn't provide a proper poll question. Please start over", "Empty question");
                await ctx.Channel.DeleteMessagesAsync(new[] { setupMessage, pollMessage });
                return;
            }

            pollEmbed.WithTitle($"**Poll: {pollQuestion}**");
            pollMessage = await pollMessage.ModifyAsync(embed: pollEmbed.Build());

            await ctx.Channel.DeleteMessageAsync(questionReponse.Result);

            setupEmbed.WithDescription(introText + secondInstruction);
            await setupMessage.ModifyAsync(embed: setupEmbed.Build());

            var timeResponse = await _interactivity.WaitForMessageAsync(msg => msg.Author == ctx.Member && msg.ChannelId == ctx.Channel.Id, _timeOut);
            if (timeResponse.TimedOut)
            {
                await ctx.ErrorAsync("You didn't respond in time. Please start over", "Timed out");
                await ctx.Channel.DeleteMessagesAsync(new[] { setupMessage, pollMessage });
                return;
            }
            _timeParser ??= new Chronic.Parser(new Chronic.Options() { Context = Chronic.Pointer.Type.Future, EndianPrecedence = Chronic.EndianPrecedence.Little, FirstDayOfWeek = DayOfWeek.Monday });
            Chronic.Span parsedTime = _timeParser.Parse(timeResponse.Result.Content);
            if (parsedTime == null)
            {
                await ctx.ErrorAsync("I couldn't understand this Date/Time. Please start over", "Invalid Time");
                await ctx.Channel.DeleteMessagesAsync(new[] { setupMessage, pollMessage });
                return;
            }
            DateTime endTime = parsedTime.ToTime();
            if (endTime < DateTime.Now)
            {
                await ctx.ErrorAsync("The provided time is in the past. Please start over", "Invalid Time");
                await ctx.Channel.DeleteMessagesAsync(new[] { setupMessage, pollMessage });
                return;
            }

            pollEmbed.WithFooter($"Poll will end on {endTime:dd.MM.yyyy} at {endTime:HH:mm \"UTC\"zz}");
            pollMessage = await pollMessage.ModifyAsync(embed: pollEmbed.Build());

            await ctx.Channel.DeleteMessageAsync(timeResponse.Result);

            setupEmbed.WithDescription(introText + thirdInstruction);
            await setupMessage.ModifyAsync(embed: setupEmbed.Build());
            await setupMessage.CreateReactionAsync(_okEmoji);

            var pollAnswers = new List<string>();
            while (true)
            {
                var addAnswerTask = _interactivity.WaitForMessageAsync(msg => msg.Author == ctx.Member && msg.ChannelId == ctx.Channel.Id, _timeOut);
                var okTask = _interactivity.WaitForReactionAsync(r => r.User == ctx.Member && r.Emoji == _okEmoji, _timeOut);
                var result = await Task.WhenAny(addAnswerTask, okTask);

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
                            await ctx.ErrorAsync("You didn't provide enough answers in time. Please start over", "Timed out");
                            await ctx.Channel.DeleteMessagesAsync(new[] { setupMessage, pollMessage });
                            return;
                        }
                    }
                    pollAnswers.Add(pollOptionResponse.Result.Content);
                    await ctx.Channel.DeleteMessageAsync(pollOptionResponse.Result);
                    pollEmbed.WithDescription(string.Join("\n", _pollService.GetEmojiMapping(pollAnswers).Select(ans => $"{ans.Key} {ans.Value}")));
                    pollMessage = await pollMessage.ModifyAsync(embed: pollEmbed.Build());
                }
                else
                {
                    if (pollAnswers.Count < 2)
                    {
                        await ctx.ErrorAsync("Not enough answer options! Please add more first!");
                    }
                    else
                    {
                        break;
                    }
                }
            }
            await ctx.Channel.DeleteMessageAsync(setupMessage);

            //Add it to the service which starts monitoring for poll reactions and adds the poll to the DB to be able to recover from Bot restarts
            Poll poll = new Poll(ctx.Guild.Id, ctx.Channel.Id, pollMessage.Id, ctx.Member.Id, pollQuestion, pollAnswers, endTime.ToUniversalTime());
            await _pollService.AddAndStartPollAsync(poll);
        }
    }
}