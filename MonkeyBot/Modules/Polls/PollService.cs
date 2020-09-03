using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using MonkeyBot.Common;
using MonkeyBot.Database;
using MonkeyBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class PollService : IPollService
    {
        private readonly DiscordClient discordClient;
        private readonly MonkeyDBContext dbContext;

        public PollService(DiscordClient discordClient, MonkeyDBContext dbContext)
        {
            this.discordClient = discordClient;
            this.dbContext = dbContext;
        }

        public async Task AddAndStartPollAsync(Poll poll)
        {
            DiscordGuild guild = await discordClient.GetGuildAsync(poll.GuildId).ConfigureAwait(false);
            DiscordChannel channel = guild?.GetChannel(poll.ChannelId);
            DiscordMessage pollMessage = await channel.GetMessageAsync(poll.MessageId).ConfigureAwait(false);
            Dictionary<DiscordEmoji, string> emojiMapping = GetEmojiMapping(poll.PossibleAnswers);

            foreach (DiscordEmoji pollEmoji in emojiMapping.Keys)
            {
                await pollMessage.CreateReactionAsync(pollEmoji).ConfigureAwait(false);
            }
            await pollMessage.PinAsync().ConfigureAwait(false);

            _ = await dbContext.Polls.AddAsync(poll).ConfigureAwait(false);
            _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);

            await StartPollAsync(poll).ConfigureAwait(false);
        }

        private async Task StartPollAsync(Poll poll)
        {
            DiscordGuild guild = await discordClient.GetGuildAsync(poll.GuildId).ConfigureAwait(false);
            DiscordChannel channel = guild?.GetChannel(poll.ChannelId);
            DiscordMember pollCreator = await guild.GetMemberAsync(poll.CreatorId).ConfigureAwait(false);
            Dictionary<DiscordEmoji, string> emojiMapping = GetEmojiMapping(poll.PossibleAnswers);

            TimeSpan pollDuration = poll.EndTimeUTC - DateTime.UtcNow;
            await Task.Delay(pollDuration).ConfigureAwait(false);
            //Only the get the message once the end time is reached to get all reactions
            DiscordMessage pollMessage = await channel.GetMessageAsync(poll.MessageId).ConfigureAwait(false);
            var pollResult = pollMessage.Reactions
                .Where(r => emojiMapping.Keys.Contains(r.Emoji))
                .Select(r => (Emoji: r.Emoji, Count: r.Count - 1))
                .ToList();
            //var pollResult = await interactivity.DoPollAsync(pollMessage, emojiMapping.Keys.ToArray(), PollBehaviour.KeepEmojis, pollDuration).ConfigureAwait(false);
            await pollMessage.UnpinAsync().ConfigureAwait(false);
            if (!pollResult.Any(r => r.Count > 0))
            {
                _ = await channel.SendMessageAsync($"No one participated in the poll {poll.Question} :(").ConfigureAwait(false);
                return;
            }
            int totalVotes = pollResult.Sum(r => r.Count);
            var pollResultEmbed = new DiscordEmbedBuilder()
                .WithTitle($"Poll results: {poll.Question}")
                .WithColor(DiscordColor.Azure)
                .WithDescription($"**{pollCreator.Mention}{(pollCreator.Username.EndsWith('s') ? "'" : "'s")} poll ended. Here are the results:**\n\n" +
                string.Join("\n", emojiMapping
                    .Select(ans => new { Answer = ans.Value, Votes = pollResult.Single(r => r.Emoji == ans.Key).Count })
                    .Select(ans => $"**{ans.Answer}**: {"vote".ToQuantity(ans.Votes)} ({100.0 * ans.Votes / totalVotes:F1} %)"))
                );
            _ = await channel.SendMessageAsync(embed: pollResultEmbed.Build()).ConfigureAwait(false);

            _ = dbContext.Polls.Remove(poll);
            _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task InitializeAsync()
        {
            List<Poll> dbPolls = await dbContext.Polls.ToListAsync().ConfigureAwait(false);
            var pastPolls = dbPolls.Where(p => p.EndTimeUTC < DateTime.UtcNow);
            //TODO: Decide with what to do with past polls. Show result if the overdue time is not too large? For now just delete from DB
            if (pastPolls.Any())
            {
                dbContext.Polls.RemoveRange(pastPolls);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            foreach (Poll poll in dbPolls.Where(p => p.EndTimeUTC > DateTime.UtcNow))
            {
                await StartPollAsync(poll).ConfigureAwait(false);
            }
        }

        public Task RemovePollAsync(Poll poll)
        {
            throw new NotImplementedException();
        }

        public Dictionary<DiscordEmoji, string> GetEmojiMapping(List<string> pollAnswers)
            => pollAnswers
                .Select((a, i) => (Key: DiscordEmoji.FromUnicode(MonkeyHelpers.GetUnicodeRegionalLetter(i)), Val: a))
                .ToDictionary(x => x.Key, x => x.Val);

    }
}
