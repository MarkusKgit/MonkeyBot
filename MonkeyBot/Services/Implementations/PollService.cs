using Discord;
using Discord.WebSocket;
using MonkeyBot.Services.Common.Poll;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Services.Implementations
{
    public class PollService : IPollService
    {
        private readonly DiscordSocketClient discordClient;

        public List<Poll> ActivePolls { get; private set; }

        public PollService(DiscordSocketClient client)
        {
            discordClient = client;
            discordClient.ReactionAdded += DiscordClient_ReactionAddedAsync;
            discordClient.ReactionRemoved += DiscordClient_ReactionRemovedAsync;
            ActivePolls = new List<Poll>();
        }

        private async Task DiscordClient_ReactionAddedAsync(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            if (!arg1.HasValue || arg2 == null || !arg3.User.IsSpecified)
                return;
            var msg = arg1.Value;
            var user = arg3.User.Value;
            var channelID = arg2.Id;
            var emote = arg3.Emote;
            if (user.IsBot)
                return;
            foreach (var poll in ActivePolls)
            {
                if (poll.ChannelId != channelID || poll.MessageId != msg.Id)
                    continue;
                if (!poll.Answers.Contains(emote))
                {
                    await msg.RemoveReactionAsync(emote, user);
                }
                if (!poll.UserReactionCount.ContainsKey(user.Id))
                    poll.UserReactionCount.Add(user.Id, 1);
                else
                    poll.UserReactionCount[user.Id]++;
                if (poll.UserReactionCount[user.Id] > 1)
                    await msg.RemoveReactionAsync(emote, user);
                return;
            }
        }

        private Task DiscordClient_ReactionRemovedAsync(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            if (!arg1.HasValue || arg2 == null || !arg3.User.IsSpecified)
                return Task.CompletedTask;
            var msg = arg1.Value;
            var user = arg3.User.Value;
            var channelID = arg2.Id;
            var emote = arg3.Emote;
            if (user.IsBot)
                return Task.CompletedTask;
            foreach (var poll in ActivePolls)
            {
                if (poll.ChannelId != channelID || poll.MessageId != msg.Id)
                    continue;
                if (poll.UserReactionCount.ContainsKey(user.Id))
                {
                    poll.UserReactionCount[user.Id]--;
                    if (poll.UserReactionCount[user.Id] <= 0)
                        poll.UserReactionCount.Remove(user.Id);
                }
            }
            return Task.CompletedTask;
        }

        public async Task AddPollAsync(Poll poll)
        {
            if (poll == null)
                return;
            var guild = discordClient.GetGuild(poll.GuildId);
            var channel = guild?.GetChannel(poll.ChannelId) as ITextChannel;
            if (channel == null)
                return;
            var msg = await channel.SendMessageAsync(poll.Question);
            if (msg != null)
            {
                foreach (var reaction in poll.Answers)
                {
                    await msg.AddReactionAsync(reaction);
                }
                poll.MessageId = msg.Id;
                ActivePolls.Add(poll);
            }
        }
    }
}