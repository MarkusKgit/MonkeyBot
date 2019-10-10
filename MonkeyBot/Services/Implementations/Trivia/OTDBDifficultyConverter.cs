using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonkeyBot.Services
{
    public class OTDBDifficultyConverter : JsonConverter<TriviaQuestionDifficulty>
    {
        public override TriviaQuestionDifficulty Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options = null)
        {
            return reader.GetString() switch {
                "easy" => TriviaQuestionDifficulty.Easy,
                "medium" => TriviaQuestionDifficulty.Medium,
                "hard" => TriviaQuestionDifficulty.Hard,
                _ => throw new ParseException("Unknown question difficulty")
            };
        }

        public override void Write(Utf8JsonWriter writer, TriviaQuestionDifficulty value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value switch
            {
                TriviaQuestionDifficulty.Easy => "easy",
                TriviaQuestionDifficulty.Medium => "medium",
                TriviaQuestionDifficulty.Hard => "hard",
                _ => throw new ArgumentException($"Didn't expect question difficulty {value}")
            });
        }
    }
}