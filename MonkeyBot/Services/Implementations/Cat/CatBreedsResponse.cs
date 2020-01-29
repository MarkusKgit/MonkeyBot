using System.Text.Json.Serialization;

namespace MonkeyBot.Services
{
    public class CatBreedsResponse
    {

        [JsonPropertyName("weight")]
        public Weight Weight { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("cfa_url")]
        public string CfaUrl { get; set; }

        [JsonPropertyName("vetstreet_url")]
        public string VetstreetUrl { get; set; }

        [JsonPropertyName("vcahospitals_url")]
        public string VcahospitalsUrl { get; set; }

        [JsonPropertyName("temperament")]
        public string Temperament { get; set; }

        [JsonPropertyName("origin")]
        public string Origin { get; set; }

        [JsonPropertyName("country_codes")]
        public string CountryCodes { get; set; }

        [JsonPropertyName("country_code")]
        public string CountryCode { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("life_span")]
        public string LifeSpan { get; set; }

        [JsonPropertyName("indoor")]
        public int Indoor { get; set; }

        [JsonPropertyName("lap")]
        public int Lap { get; set; }

        [JsonPropertyName("alt_names")]
        public string AltNames { get; set; }

        [JsonPropertyName("adaptability")]
        public int Adaptability { get; set; }

        [JsonPropertyName("affection_level")]
        public int AffectionLevel { get; set; }

        [JsonPropertyName("child_friendly")]
        public int ChildFriendly { get; set; }

        [JsonPropertyName("dog_friendly")]
        public int DogFriendly { get; set; }

        [JsonPropertyName("energy_level")]
        public int EnergyLevel { get; set; }

        [JsonPropertyName("grooming")]
        public int Grooming { get; set; }

        [JsonPropertyName("health_issues")]
        public int HealthIssues { get; set; }

        [JsonPropertyName("intelligence")]
        public int Intelligence { get; set; }

        [JsonPropertyName("shedding_level")]
        public int SheddingLevel { get; set; }

        [JsonPropertyName("social_needs")]
        public int SocialNeeds { get; set; }

        [JsonPropertyName("stranger_friendly")]
        public int StrangerFriendly { get; set; }

        [JsonPropertyName("vocalisation")]
        public int Vocalisation { get; set; }

        [JsonPropertyName("experimental")]
        public int Experimental { get; set; }

        [JsonPropertyName("hairless")]
        public int Hairless { get; set; }

        [JsonPropertyName("natural")]
        public int Natural { get; set; }

        [JsonPropertyName("rare")]
        public int Rare { get; set; }

        [JsonPropertyName("rex")]
        public int Rex { get; set; }

        [JsonPropertyName("suppressed_tail")]
        public int SuppressedTail { get; set; }

        [JsonPropertyName("short_legs")]
        public int ShortLegs { get; set; }

        [JsonPropertyName("wikipedia_url")]
        public string WikipediaUrl { get; set; }

        [JsonPropertyName("hypoallergenic")]
        public int Hypoallergenic { get; set; }

        [JsonPropertyName("cat_friendly")]
        public int? CatFriendly { get; set; }

        [JsonPropertyName("bidability")]
        public int? Bidability { get; set; }
    }

    public class Weight
    {
        [JsonPropertyName("imperial")]
        public string Imperial { get; set; }

        [JsonPropertyName("metric")]
        public string Metric { get; set; }
    }
}
