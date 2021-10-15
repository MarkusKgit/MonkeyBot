namespace MonkeyBot.Models
{
    public class MessageComponentLink
    {
        public int Id { get; set; }

        public ulong GuildId { get; set; }

        public ulong ChannelId { get; set; }

        public ulong ParentMessageId { get; set; }

        public ulong MessageId { get; set; }

        public string ComponentId { get; set; }
    }
}
