using DSharpPlus.Entities;
using MonkeyBot.Common;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace MonkeyBot.Models
{
    public class PollAnswer
    {
        [JsonIgnore]
        public string Id => $"answer_{OrderNumber}";
        public int OrderNumber { get; }
        public string Value { get; }
        public ISet<ulong> UsersWhoVoted { get; }
        [JsonIgnore]
        public int Count => UsersWhoVoted.Count;
        [JsonIgnore]
        public DiscordEmoji Emoji => DiscordEmoji.FromUnicode(MonkeyHelpers.GetUnicodeRegionalLetter(OrderNumber));

        [JsonConstructor]
        public PollAnswer(int orderNumber, string value, ISet<ulong> usersWhoVoted = null)
        {
            OrderNumber = orderNumber;
            Value = value;
            UsersWhoVoted = usersWhoVoted?.ToHashSet() ?? new HashSet<ulong>();
        }
        
        public void UpdateCount(ulong userId)
        {
            if (UsersWhoVoted.Contains(userId))
            {
                UsersWhoVoted.Remove(userId);
            }
            else
            {
                UsersWhoVoted.Add(userId);
            }
        }
    }
}