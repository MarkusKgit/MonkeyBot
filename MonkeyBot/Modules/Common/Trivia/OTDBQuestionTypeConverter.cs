using Newtonsoft.Json;
using System;

namespace MonkeyBot.Modules.Common.Trivia
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
                return TriviaQuestionType.TrueFalse;
            else if ((string)reader.Value == "multiple")
                return TriviaQuestionType.MultipleChoice;
            else
                return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            TriviaQuestionType type = (TriviaQuestionType)value;
            switch (type)
            {
                case TriviaQuestionType.TrueFalse:
                    writer.WriteValue("boolean");
                    break;

                case TriviaQuestionType.MultipleChoice:
                    writer.WriteValue("multiple");
                    break;

                default:
                    break;
            }
        }
    }
}