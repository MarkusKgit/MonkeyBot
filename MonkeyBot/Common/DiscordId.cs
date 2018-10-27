using System;

namespace MonkeyBot.Common
{
    /// <summary>
    /// Provides a unique id as a combination of GuildId, ChannelId and UserId. Id parts that are null and will be ignored
    /// </summary>
    public class DiscordId : IEquatable<DiscordId>
    {
        public ulong? GuildId { get; set; }
        public ulong? ChannelId { get; set; }
        public ulong? UserId { get; set; }

        public DiscordId()
        {
        }

        public DiscordId(ulong? guildId, ulong? channelId, ulong? userId)
        {
            if (guildId == null && channelId == null && userId == null)
                throw new ArgumentNullException("Provide at least one id");
            GuildId = guildId;
            ChannelId = channelId;
            UserId = userId;
        }

        public override bool Equals(object obj)
        {
            return (obj is DiscordId) && Equals(obj as DiscordId);
        }

        public bool Equals(DiscordId other)
        {
            //if both IDs have a value then the values must be equal or either one has no value (is null) then this id part can be ignored
            bool guild = ((this.GuildId.HasValue && other.GuildId.HasValue && (this.GuildId == other.GuildId)) || (!this.GuildId.HasValue || !other.GuildId.HasValue));
            bool channel = ((this.ChannelId.HasValue && other.ChannelId.HasValue && (this.ChannelId == other.ChannelId)) || (!this.ChannelId.HasValue || !other.ChannelId.HasValue));
            bool user = ((this.UserId.HasValue && other.UserId.HasValue && (this.UserId == other.UserId)) || (!this.UserId.HasValue || !other.UserId.HasValue));
            return (guild && channel && user);
        }

        public override int GetHashCode()
        {
            return (int)(GuildId ?? 0) + (int)(ChannelId ?? 0) + (int)(UserId ?? 0);
        }

        public static bool operator ==(DiscordId lhs, DiscordId rhs) => lhs.Equals(rhs);

        public static bool operator !=(DiscordId lhs, DiscordId rhs) => !lhs.Equals(rhs);
    }
}