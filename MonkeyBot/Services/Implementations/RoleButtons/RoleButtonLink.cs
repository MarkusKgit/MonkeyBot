namespace MonkeyBot.Services
{
    public class RoleButtonLink
    {
        public ulong GuildId { get; private set; }

        public ulong MessageId { get; private set; }

        public ulong RoleId { get; private set; }

        public string EmoteString { get; set; }

        public RoleButtonLink()
        {
        }

        public RoleButtonLink(ulong guildId, ulong messageId, ulong roleId, string emoteString)
        {
            GuildId = guildId;
            MessageId = messageId;
            RoleId = roleId;
            EmoteString = emoteString;
        }
    }
}