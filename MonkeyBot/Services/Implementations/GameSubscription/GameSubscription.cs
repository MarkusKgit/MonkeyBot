namespace MonkeyBot.Services
{
    public class GameSubscription
    {
        public ulong GuildId { get; private set; }

        public ulong UserId { get; private set; }

        public string GameName { get; private set; }

        public GameSubscription(ulong guildId, ulong userId, string gameName)
        {
            GuildId = guildId;
            UserId = userId;
            GameName = gameName;
        }
    }
}