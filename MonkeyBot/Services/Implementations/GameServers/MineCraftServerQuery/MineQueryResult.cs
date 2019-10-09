using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MonkeyBot.Services
{
    public class MineQueryResult
    {
        /// <summary>
        /// Protocol that the server is using and the given name
        /// </summary>
        [JsonPropertyName("description")]
        public DescriptionPayload Description { get; set; }

        [JsonPropertyName("players")]
        public PlayersPayload Players { get; set; }

        [JsonPropertyName("version")]
        public VersionPayload Version { get; set; }

        [JsonPropertyName("modinfo")]
        public ModInfosPayload ModInfos { get; set; }
    }

    public class DescriptionPayload
    {
        [JsonPropertyName("text")]
        public string Motd { get; set; }
    }

    public class PlayersPayload
    {
        [JsonPropertyName("max")]
        public int Max { get; set; }

        [JsonPropertyName("online")]
        public int Online { get; set; }

        [JsonPropertyName("sample")]
        public List<PlayerPayload> Sample { get; set; }
    }

    public class VersionPayload
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("protocol")]
        public int Protocol { get; set; }
    }

    public class PlayerPayload
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }
    }

    public class ModInfosPayload
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("modList")]
        public List<ModInfoPayload> ModList { get; set; }
    }

    public class ModInfoPayload
    {
        [JsonPropertyName("modid")]
        public string Id { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }
    }
}