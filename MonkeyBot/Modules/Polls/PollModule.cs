using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity.Extensions;
using MonkeyBot.Common;
using MonkeyBot.Models;
using MonkeyBot.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [Description("Simple poll")]
    [RequireGuild]
    [MinPermissions(AccessLevel.User)]
    public class PollModule : BaseCommandModule
    {
        private readonly IPollService _pollService;

        private static readonly TimeSpan _timeOut = TimeSpan.FromSeconds(60);

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
            var pollMessageUpdater = await PollMessageUpdater.Create(ctx);

            var setupMessageUpdater = await SetupMessageUpdater.Create(ctx);

            var interactivity = ctx.Client.GetInteractivity();

            var questionReponse =
                await interactivity.WaitForMessageAsync(
                    msg => msg.Author == ctx.Member && msg.ChannelId == ctx.Channel.Id,
                    _timeOut);

            if (questionReponse.TimedOut)
            {
                await ctx.ErrorAsync("You didn't respond in time. Please start over", "Timed out");
                await ctx.Channel.DeleteMessagesAsync(new[] {setupMessageUpdater.Message, pollMessageUpdater.Message});
                return;
            }

            string pollQuestion = questionReponse.Result.Content.Trim();
            if (pollQuestion.IsEmptyOrWhiteSpace())
            {
                await ctx.ErrorAsync("You didn't provide a proper poll question. Please start over", "Empty question");
                await ctx.Channel.DeleteMessagesAsync(new[] {setupMessageUpdater.Message, pollMessageUpdater.Message});
                return;
            }

            await pollMessageUpdater.SetPollTitle(pollQuestion);

            await ctx.Channel.DeleteMessageAsync(questionReponse.Result);

            await setupMessageUpdater.SetSecondInstruction();

            var timeResponse =
                await interactivity.WaitForMessageAsync(
                    msg => msg.Author == ctx.Member && msg.ChannelId == ctx.Channel.Id,
                    _timeOut);
            if (timeResponse.TimedOut)
            {
                await ctx.ErrorAsync("You didn't respond in time. Please start over", "Timed out");
                await ctx.Channel.DeleteMessagesAsync(new[] {setupMessageUpdater.Message, pollMessageUpdater.Message});
                return;
            }

            var timeParser = new Chronic.Parser(new Chronic.Options()
            {
                Context = Chronic.Pointer.Type.Future,
                EndianPrecedence = Chronic.EndianPrecedence.Little,
                FirstDayOfWeek = DayOfWeek.Monday
            });
            Chronic.Span parsedTime = timeParser.Parse(timeResponse.Result.Content);
            if (parsedTime == null)
            {
                await ctx.ErrorAsync("I couldn't understand this Date/Time. Please start over", "Invalid Time");
                await ctx.Channel.DeleteMessagesAsync(new[] {setupMessageUpdater.Message, pollMessageUpdater.Message});
                return;
            }

            DateTime endTime = parsedTime.ToTime();
            if (endTime < DateTime.Now)
            {
                await ctx.ErrorAsync("The provided time is in the past. Please start over", "Invalid Time");
                await ctx.Channel.DeleteMessagesAsync(new[] {setupMessageUpdater.Message, pollMessageUpdater.Message});
                return;
            }

            await pollMessageUpdater.SetEndTime(endTime);

            await ctx.Channel.DeleteMessageAsync(timeResponse.Result);

            await setupMessageUpdater.SetThirdInstruction();
            await setupMessageUpdater.AddOkButton();

            var pollAnswers = new List<PollAnswer>();

            while (true)
            {
                var addAnswerTask = interactivity.WaitForMessageAsync(
                    msg => msg.Author == ctx.Member && msg.ChannelId == ctx.Channel.Id,
                    _timeOut);
                var okTask = interactivity.WaitForButtonAsync(setupMessageUpdater.Message, ctx.Member, _timeOut);
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

                        await ctx.ErrorAsync("You didn't provide enough answers in time. Please start over",
                            "Timed out");
                        await ctx.Channel.DeleteMessagesAsync(new[]
                        {
                            setupMessageUpdater.Message, pollMessageUpdater.Message
                        });
                        return;
                    }

                    var order = pollAnswers.Count;
                    var answerValue = pollOptionResponse.Result.Content;
                    pollAnswers.Add(new PollAnswer(order, answerValue));

                    await ctx.Channel.DeleteMessageAsync(pollOptionResponse.Result);

                    await pollMessageUpdater.UpdateAnswers(pollAnswers);
                    await pollMessageUpdater.UpdateAnswersButtons(pollAnswers);
                }
                else
                {
                    if (!okTask.Result.TimedOut)
                    {
                        await okTask.Result.Result.Interaction.CreateResponseAsync(InteractionResponseType
                            .UpdateMessage);
                        if (pollAnswers.Count < 2)
                        {
                            await ctx.ErrorAsync("Not enough answer options! Please add more first!");
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (pollAnswers.Count >= 2)
                        {
                            break;
                        }

                        await ctx.ErrorAsync("You didn't provide enough answers in time. Please start over",
                            "Timed out");
                        await ctx.Channel.DeleteMessagesAsync(new[]
                        {
                            setupMessageUpdater.Message, pollMessageUpdater.Message
                        });
                        return;
                    }
                }
            }

            await ctx.Channel.DeleteMessageAsync(setupMessageUpdater.Message);

            var poll = new Poll(ctx.Guild.Id, ctx.Channel.Id, pollMessageUpdater.Message.Id, ctx.Member.Id,
                pollQuestion, pollAnswers,
                endTime.ToUniversalTime());

            await _pollService.AddAndStartPollAsync(poll);
        }
    }
}