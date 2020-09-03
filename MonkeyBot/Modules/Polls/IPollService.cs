using DSharpPlus.Entities;
using MonkeyBot.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IPollService
    {
        Task AddAndStartPollAsync(Poll poll);

        Task InitializeAsync();

        Dictionary<DiscordEmoji, string> GetEmojiMapping(List<string> pollAnswers);
    }
}
