namespace MonkeyBot.Services
{
    public class GuildFeed
    {
        public string Name { get; }
        public string Url { get; }
        public ulong ChannelId { get; }

        public GuildFeed(string name, string feedUrl, ulong feedChannelId)
        {
            Name = name;
            Url = feedUrl;
            ChannelId = feedChannelId;
        }
    }
}
