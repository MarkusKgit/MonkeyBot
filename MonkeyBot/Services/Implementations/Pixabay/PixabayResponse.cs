using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MonkeyBot.Services
{
    public class PixabayResponse
    {

        [JsonPropertyName("totalHits")]
        public int TotalHits { get; set; }

        [JsonPropertyName("hits")]
        public IList<Hit> Hits { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }

    public class Hit
    {
        [JsonPropertyName("largeImageURL")]
        public string LargeImageURL { get; set; }

        [JsonPropertyName("webformatHeight")]
        public int WebformatHeight { get; set; }

        [JsonPropertyName("webformatWidth")]
        public int WebformatWidth { get; set; }

        [JsonPropertyName("likes")]
        public int Likes { get; set; }

        [JsonPropertyName("imageWidth")]
        public int ImageWidth { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("views")]
        public int Views { get; set; }

        [JsonPropertyName("comments")]
        public int Comments { get; set; }

        [JsonPropertyName("pageURL")]
        public string PageURL { get; set; }

        [JsonPropertyName("imageHeight")]
        public int ImageHeight { get; set; }

        [JsonPropertyName("webformatURL")]
        public string WebformatURL { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("previewHeight")]
        public int PreviewHeight { get; set; }

        [JsonPropertyName("tags")]
        public string Tags { get; set; }

        [JsonPropertyName("downloads")]
        public int Downloads { get; set; }

        [JsonPropertyName("user")]
        public string User { get; set; }

        [JsonPropertyName("favorites")]
        public int Favorites { get; set; }

        [JsonPropertyName("imageSize")]
        public int ImageSize { get; set; }

        [JsonPropertyName("previewWidth")]
        public int PreviewWidth { get; set; }

        [JsonPropertyName("userImageURL")]
        public string UserImageURL { get; set; }

        [JsonPropertyName("previewURL")]
        public string PreviewURL { get; set; }
    }
}
