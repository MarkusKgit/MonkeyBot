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
        private DiscordMessage _pollMessage;
        private DiscordMessageBuilder _pollPreviewMessage;
        private DiscordEmbedBuilder _pollPreviewMessageEmbed;

        private PollMessageUpdater(CommandContext ctx, DiscordEmbedBuilder pollPreviewMessageEmbed,
            DiscordMessage pollMessage)
        {
            _pollMessage = pollMessage;
            _pollPreviewMessageEmbed = pollPreviewMessageEmbed;

            _pollPreviewMessage = new DiscordMessageBuilder()
                .WithEmbed(_pollPreviewMessageEmbed.Build());
        }

        public DiscordMessage Message => _pollMessage;

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
                .WithAuthor(ctx.Member.Username, iconUrl: ctx.Member.AvatarUrl);
            var pollMessage = await ctx.RespondAsync(pollEmbedBuilder.Build());
            return new PollMessageUpdater(ctx, pollEmbedBuilder, pollMessage);
        }


        public async Task SetPollTitle(string title) => await WithTitle($"**Poll: {title}**");

        public async Task SetEndTime(DateTime endTime) => await WithFooter($"Poll will end on {endTime:dd.MM.yyyy} at {endTime:HH:mm \"UTC\"zz}");

        public async Task UpdateAnswers(List<PollAnswer> pollAnswers) => await WithDescription(BuildDescription(pollAnswers));

        public async Task UpdateAnswersButtons(List<PollAnswer> pollAnswers) => await WithComponents(BuildAnswerButtons(pollAnswers));

        private async Task WithTitle(string title)
        {
            _pollPreviewMessageEmbed = _pollPreviewMessageEmbed.WithTitle(title);
            _pollPreviewMessage = _pollPreviewMessage.WithEmbed(_pollPreviewMessageEmbed);
            _pollMessage = await _pollMessage.ModifyAsync(_pollPreviewMessage);
        }

        private async Task WithFooter(string footer)
        {
            _pollPreviewMessageEmbed = _pollPreviewMessageEmbed.WithFooter(footer);
            _pollPreviewMessage = _pollPreviewMessage.WithEmbed(_pollPreviewMessageEmbed);
            _pollMessage = await _pollMessage.ModifyAsync(_pollPreviewMessage);
        }

        private async Task WithDescription(string description)
        {
            _pollPreviewMessageEmbed = _pollPreviewMessageEmbed.WithDescription(description);
            _pollPreviewMessage = _pollPreviewMessage.WithEmbed(_pollPreviewMessageEmbed);
            _pollMessage = await _pollMessage.ModifyAsync(_pollPreviewMessage);
        }

        private async Task WithComponents(IEnumerable<DiscordActionRowComponent> components)
        {
            _pollPreviewMessage.ClearComponents();
            _pollPreviewMessage = _pollPreviewMessage
                .WithEmbed(_pollPreviewMessageEmbed)
                .AddComponents(components);
            _pollMessage = await _pollMessage.ModifyAsync(_pollPreviewMessage);
        }
    }
}