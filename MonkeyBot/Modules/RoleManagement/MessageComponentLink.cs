namespace MonkeyBot.Models
{
    public class MessageComponentLink
    {
        public int ID { get; set; }

        public ulong GuildID { get; set; }

        public ulong ChannelID { get; set; }

        public ulong MessageID { get; set; }

        public string ComponentId { get; set; }
    }
}
