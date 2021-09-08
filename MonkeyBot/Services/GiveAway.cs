using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonkeyBot.Services
{
    public class GiveAway
    {
        public string Title { get; set; }

        public string Worth { get; set; }

        [JsonPropertyName("thumbnail")]
        public string ThumbnailUrl { get; set; }

        [JsonPropertyName("image")]
        public string ImageUrl { get; set; }

        public string Description { get; set; }

        public string Instructions { get; set; }

        [JsonPropertyName("Open_giveaway_url")]
        public string GiveAwayUrl { get; set; }

        [JsonPropertyName("published_date")]
        [JsonConverter(typeof(DateTimeConverter))]
        public DateTime PublishedDate { get; set; }

        [JsonPropertyName("end_date")]
        [JsonConverter(typeof(NullableDateTimeConverter))]
        public DateTime? EndDate { get; set; }

        internal class DateTimeConverter : JsonConverter<DateTime>
        {
            public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => DateTime.Parse(reader.GetString());
            public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) => throw new NotImplementedException();
        }

        internal class NullableDateTimeConverter : JsonConverter<DateTime?>
        {
            public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => DateTime.TryParse(reader.GetString(), out var date) ? date : null;
            public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options) => throw new NotImplementedException();
        }
    }
}