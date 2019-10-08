using Newtonsoft.Json;
using System;

namespace MonkeyBot.Services
{
    public class OTDBDifficultyConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(string);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return (string)reader.Value switch
            {
                "easy" => TriviaQuestionDifficulty.Easy,
                "medium" => TriviaQuestionDifficulty.Medium,
                "hard" => TriviaQuestionDifficulty.Hard,
                _ => throw new ParseException("Unknown question difficulty")
            };
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var difficulty = (TriviaQuestionDifficulty)value;
            writer.WriteValue(difficulty switch
            {
                TriviaQuestionDifficulty.Easy => "easy",
                TriviaQuestionDifficulty.Medium => "medium",
                TriviaQuestionDifficulty.Hard => "hard",
                _ => throw new ArgumentException($"Didn't expect question difficulty {difficulty}")
            });
        }
    }
}