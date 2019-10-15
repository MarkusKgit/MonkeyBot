namespace MonkeyBot.Models
{
    public class GameSubscription
    {
        public int ID { get; set; }

        public ulong GuildID { get; set; }

        public ulong UserID { get; set; }

        public string? GameName { get; set; }
    }
}