﻿using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using MonkeyBot.Common;
using MonkeyBot.Database;
using MonkeyBot.Models;
using MonkeyBot.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class PollService : IPollService
    {
        private readonly DiscordClient _discordClient;
        private readonly IDbContextFactory<MonkeyDBContext> _dbContextFactory;

        public PollService(DiscordClient discordClient, IDbContextFactory<MonkeyDBContext> dbContextFactory)
        {
            _discordClient = discordClient;
            _dbContextFactory = dbContextFactory;
        }

        public async Task AddAndStartPollAsync(Poll poll)
        {
            DiscordGuild guild = await _discordClient.GetGuildAsync(poll.GuildId);
            DiscordChannel channel = guild?.GetChannel(poll.ChannelId);
            DiscordMessage pollMessage = await channel.GetMessageAsync(poll.MessageId);
            
            await pollMessage.PinAsync();

            using var dbContext = _dbContextFactory.CreateDbContext();
            await dbContext.Polls.AddAsync(poll);
            await dbContext.SaveChangesAsync();

            await StartPollAsync(poll);
        }
        
        private async Task StartPollAsync(Poll poll)
        {
            DiscordGuild guild = await _discordClient.GetGuildAsync(poll.GuildId);
            DiscordChannel channel = guild?.GetChannel(poll.ChannelId);
            DiscordMember pollCreator = await guild.GetMemberAsync(poll.CreatorId);
            
            DiscordMessage pollMessage = await channel.TryGetMessageAsync(poll.MessageId);
            if (pollMessage == null)
                return;
            pollMessage = await pollMessage.ModifyAsync(x => x.WithEmbed(pollMessage.Embeds.First()).AddComponents(PollMessageUpdater.BuildAnswerButtons(poll.PossibleAnswers)));
            var pollMessageUpdater = PollMessageUpdater.Create(pollMessage);
            
            TimeSpan pollDuration = poll.EndTimeUTC - DateTime.UtcNow;
            var cancelTokenSource = new CancellationTokenSource();
            cancelTokenSource.CancelAfter(pollDuration);
            
            while (!cancelTokenSource.IsCancellationRequested)
            {
                var btnClick = await pollMessage.WaitForButtonAsync(cancelTokenSource.Token);
                if (!btnClick.TimedOut)
                {
                    var user = btnClick.Result.User;
                    var answerId = btnClick.Result.Id;

                    var answer = poll.PossibleAnswers.First(x => x.Id == answerId);
                    
                    answer.UpdateCount(user.Id);
                    using var dbContext = _dbContextFactory.CreateDbContext();
                    dbContext.Update(poll);
                    await  dbContext.SaveChangesAsync(cancelTokenSource.Token);
                    
                    await btnClick.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage);
                    
                    await pollMessageUpdater.UpdateAnswers(poll.PossibleAnswers);
                }
            }

            var pollResult = poll.PossibleAnswers
                .Select(r => (Emoji: r.Emoji, Count: r.Count))
                .ToList();
            
            await pollMessageUpdater.SetAsEnded(poll.EndTimeUTC);
            await pollMessage.UnpinAsync();
            if (!pollResult.Any(r => r.Count > 0))
            {
                await channel.SendMessageAsync($"No one participated in the poll {poll.Question} :(");
                return;
            }

            Dictionary<DiscordEmoji, string> emojiMapping = GetEmojiMapping(poll.PossibleAnswers.Select(x=>x.Value).ToList());
            int totalVotes = pollResult.Sum(r => r.Count);
            var pollResultEmbed = new DiscordEmbedBuilder()
                .WithTitle($"Poll results: {poll.Question}")
                .WithColor(DiscordColor.Azure)
                .WithDescription(
                    $"**{pollCreator.Mention}{(pollCreator.DisplayName.EndsWith('s') ? "'" : "'s")} poll ended. Here are the results:**\n\n" +
                    string.Join("\n", emojiMapping
                        .Select(ans =>
                            new {Answer = ans.Value, Votes = pollResult.Single(r => r.Emoji == ans.Key).Count})
                        .Select(ans =>
                            $"**{ans.Answer}**: {"vote".ToQuantity(ans.Votes)} ({100.0 * ans.Votes / totalVotes:F1} %)"))
                );
            await channel.SendMessageAsync(embed: pollResultEmbed.Build());

            using var dbContext1 = _dbContextFactory.CreateDbContext();
            dbContext1.Polls.Remove(poll);
            await dbContext1.SaveChangesAsync();
        }

        public async Task InitializeAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            List<Poll> dbPolls = await dbContext.Polls.ToListAsync();
            var pastPolls = dbPolls.Where(p => p.EndTimeUTC < DateTime.UtcNow);
            //TODO: Decide with what to do with past polls. Show result if the overdue time is not too large? For now just delete from DB
            if (pastPolls.Any())
            {                
                dbContext.Polls.RemoveRange(pastPolls);
                await dbContext.SaveChangesAsync();
            }

            var pollsTasks = dbPolls.Where(p => p.EndTimeUTC > DateTime.UtcNow)
                .Select(StartPollAsync);
            await Task.WhenAll(pollsTasks);
        }

        public Task RemovePollAsync(Poll poll)
        {
            //TODO: Implement
            throw new NotImplementedException();
        }

        private static Dictionary<DiscordEmoji, string> GetEmojiMapping(IEnumerable<string> pollAnswers)
            => pollAnswers
                .Select((a, i) => (Key: DiscordEmoji.FromUnicode(MonkeyHelpers.GetUnicodeRegionalLetter(i)), Val: a))
                .ToDictionary(x => x.Key, x => x.Val);
    }
}