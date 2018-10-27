namespace MonkeyBot.Services.Common.MineCraftServerQuery
{
    public class MineStats
    {
        public string Version { get; }
        public string Motd { get; }
        public string CurrentPlayers { get; }
        public string MaximumPlayers { get; }

        public MineStats(string version, string motd, string currentPlayers, string maximumPlayers)
        {
            Motd = motd;
            Version = version;
            CurrentPlayers = currentPlayers;
            MaximumPlayers = maximumPlayers;
        }
    }
}