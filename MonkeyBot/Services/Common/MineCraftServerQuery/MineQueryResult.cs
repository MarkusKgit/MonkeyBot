using Newtonsoft.Json;
using System.Collections.Generic;

namespace MonkeyBot.Services.Common.MineCraftServerQuery
{
    public class MineQueryResult
    {
        /// <summary>
        /// Protocol that the server is using and the given name
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        public DescriptionPayload Description { get; set; }

        [JsonProperty(PropertyName = "players")]
        public PlayersPayload Players { get; set; }

        [JsonProperty(PropertyName = "version")]
        public VersionPayload Version { get; set; }

        [JsonProperty(PropertyName = "modinfo")]
        public ModInfosPayload ModInfos { get; set; }
    }

    public class DescriptionPayload
    {
        [JsonProperty(PropertyName = "text")]
        public string Motd { get; set; }
    }

    public class PlayersPayload
    {
        [JsonProperty(PropertyName = "max")]
        public int Max { get; set; }

        [JsonProperty(PropertyName = "online")]
        public int Online { get; set; }

        [JsonProperty(PropertyName = "sample")]
        public List<PlayerPayload> Sample { get; set; }
    }

    public class VersionPayload
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "protocol")]
        public int Protocol { get; set; }
    }

    public class PlayerPayload
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }

    public class ModInfosPayload
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "modList")]
        public List<ModInfoPayload> ModList { get; set; }
    }

    public class ModInfoPayload
    {
        [JsonProperty(PropertyName = "modid")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; }
    }
}