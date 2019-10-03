using Newtonsoft.Json;
using System;

namespace MonkeyBot.Services
{
    public class OTDBDifficultyConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if ((string)reader.Value == "easy")
                return TriviaQuestionDifficulty.Easy;
            else if ((string)reader.Value == "medium")
                return TriviaQuestionDifficulty.Medium;
            else if ((string)reader.Value == "hard")
                return TriviaQuestionDifficulty.Hard;
            else
                return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var difficulty = (TriviaQuestionDifficulty)value;
            switch (difficulty)
            {
                case TriviaQuestionDifficulty.Easy:
                    writer.WriteValue("easy");
                    break;

                case TriviaQuestionDifficulty.Medium:
                    writer.WriteValue("medium");
                    break;

                case TriviaQuestionDifficulty.Hard:
                    writer.WriteValue("hard");
                    break;

                default:
                    break;
            }
        }
    }
}