using Newtonsoft.Json;
using System;

namespace MonkeyBot.Trivia
{
    public class OTDBDifficultyConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(string));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if ((string)reader.Value == "easy")
                return QuestionDifficulty.Easy;
            else if ((string)reader.Value == "medium")
                return QuestionDifficulty.Medium;
            else if ((string)reader.Value == "hard")
                return QuestionDifficulty.Hard;
            else
                return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            QuestionDifficulty difficulty = (QuestionDifficulty)value;
            switch (difficulty)
            {
                case QuestionDifficulty.Easy:
                    writer.WriteValue("easy");
                    break;

                case QuestionDifficulty.Medium:
                    writer.WriteValue("medium");
                    break;

                case QuestionDifficulty.Hard:
                    writer.WriteValue("hard");
                    break;

                default:
                    break;
            }
        }
    }
}