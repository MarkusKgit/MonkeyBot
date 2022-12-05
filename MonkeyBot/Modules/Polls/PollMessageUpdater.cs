using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using MonkeyBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    public class PollMessageUpdater
    {
        private DiscordEmbedBuilder _embedBuilder;
        private DiscordMessageBuilder _messageBuilder;
        private DiscordMessage _message;

        private PollMessageUpdater(DiscordEmbedBuilder embedBuilder,
            DiscordMessageBuilder messageBuilder, DiscordMessage message)
        {
            _embedBuilder = embedBuilder;
            _messageBuilder = messageBuilder;
            _message = message;
        }

        public DiscordMessage Message => _message;

        public static string BuildDescription(IEnumerable<PollAnswer> pollAnswers) =>
            string.Join("\n",
                pollAnswers.Select(ans => $"{ans.Emoji}`[{ans.Count}]` {ans.Value}"));

        public static IEnumerable<DiscordActionRowComponent>
            BuildAnswerButtons(IEnumerable<PollAnswer> pollAnswers) =>
            pollAnswers
                .GroupBy(x => x.OrderNumber / 5)
                .Select(g => new DiscordActionRowComponent(g.Select(x => new DiscordButtonComponent(
                    ButtonStyle.Primary, x.Id, string.Empty,
                    emoji: new DiscordComponentEmoji(x.Emoji)))));
        
        public static async Task<PollMessageUpdater> Create(CommandContext ctx)
        {
            var pollEmbedBuilder = new DiscordEmbedBuilder()
                .WithTitle("New poll")
                .WithColor(DiscordColor.Azure)
                .WithDescription("...")
                .WithAuthor(ctx.Member.DisplayName, iconUrl: ctx.Member.AvatarUrl);
            var pollMessage = await ctx.RespondAsync(pollEmbedBuilder.Build());
            var msgBuilder = new DiscordMessageBuilder()
                .WithEmbed(pollEmbedBuilder.Build());
            return new PollMessageUpdater(pollEmbedBuilder, msgBuilder, pollMessage);
        }

        public static PollMessageUpdater Create(DiscordMessage message)
        {
            var embedBuilder = new DiscordEmbedBuilder(message.Embeds.Single());
            var msgBuilder = new DiscordMessageBuilder()
                .WithEmbed(embedBuilder.Build())
                .AddComponents(message.Components);
            
            return new PollMessageUpdater(embedBuilder, msgBuilder, message);
        }
        
        public async Task SetPollTitle(string title) => await WithTitle($"**Poll: {title}**");

        public async Task SetEndTime(DateTime endTime) => await WithField("End Time", $"Poll will end on {Formatter.Timestamp(endTime, TimestampFormat.ShortDate)} at {Formatter.Timestamp(endTime, TimestampFormat.ShortTime)}");

        public async Task UpdateAnswers(List<PollAnswer> pollAnswers) => await WithDescription(BuildDescription(pollAnswers));
        
        public async Task UpdateAnswersButtons(List<PollAnswer> pollAnswers) => await WithComponents(BuildAnswerButtons(pollAnswers));

        public async Task SetAsEnded(DateTime endTime)
        {
            _embedBuilder =
                _embedBuilder.WithFooter(
                    $"Poll ended on {Formatter.Timestamp(endTime, TimestampFormat.ShortDate)} at {Formatter.Timestamp(endTime, TimestampFormat.ShortTime)}");
            _messageBuilder.ClearComponents();
            _messageBuilder = _messageBuilder.WithEmbed(_embedBuilder.Build());
            _message = await _message.ModifyAsync(_messageBuilder);
        }

        private async Task WithTitle(string title)
        {
            _embedBuilder = _embedBuilder.WithTitle(title);
            _messageBuilder = _messageBuilder.WithEmbed(_embedBuilder);
            _message = await _message.ModifyAsync(_messageBuilder);
        }

        private async Task WithField(string title, string description)
        {
            _embedBuilder = _embedBuilder.AddField(title, description);
            _messageBuilder = _messageBuilder.WithEmbed(_embedBuilder);
            _message = await _message.ModifyAsync(_messageBuilder);
        }

        private async Task WithDescription(string description)
        {
            _embedBuilder = _embedBuilder.WithDescription(description);
            _messageBuilder = _messageBuilder.WithEmbed(_embedBuilder);
            _message = await _message.ModifyAsync(_messageBuilder);
        }

        private async Task WithComponents(IEnumerable<DiscordActionRowComponent> components)
        {
            _messageBuilder.ClearComponents();
            _messageBuilder = _messageBuilder
                .WithEmbed(_embedBuilder)
                .AddComponents(components);
            _message = await _message.ModifyAsync(_messageBuilder);
        }
    }
}