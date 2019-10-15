namespace MonkeyBot.Models
{
    public class RoleButtonLink
    {
        public int ID { get; set; }

        public ulong GuildID { get; set; }

        public ulong MessageID { get; set; }

        public ulong RoleID { get; set; }

        public string? EmoteString { get; set; }
    }
}