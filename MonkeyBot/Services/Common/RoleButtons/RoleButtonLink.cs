namespace MonkeyBot.Services.Common.RoleButtons
{
    public class RoleButtonLink
    {
        public ulong GuildId { get; private set; }

        public ulong MessageId { get; private set; }

        public ulong RoleId { get; private set; }

        public string Emote { get; set; }

        public RoleButtonLink(ulong guildId, ulong messageId, ulong roleId, string emoji)
        {
            GuildId = guildId;
            MessageId = messageId;
            RoleId = roleId;
            Emote = emoji;
        }
    }
}