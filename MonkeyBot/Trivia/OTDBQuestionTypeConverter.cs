using Newtonsoft.Json;
using System;

namespace MonkeyBot.Trivia
{
    public class OTDBQuestionTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(string));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if ((string)reader.Value == "boolean")
                return QuestionType.TrueFalse;
            else if ((string)reader.Value == "multiple")
                return QuestionType.MultipleChoice;
            else
                return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            QuestionType type = (QuestionType)value;
            switch (type)
            {
                case QuestionType.TrueFalse:
                    writer.WriteValue("boolean");
                    break;

                case QuestionType.MultipleChoice:
                    writer.WriteValue("multiple");
                    break;

                default:
                    break;
            }
        }
    }
}