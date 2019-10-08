using Newtonsoft.Json;
using System;

namespace MonkeyBot.Services
{
    public class OTDBQuestionTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(string);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return (string)reader.Value switch
            {
                "boolean" => TriviaQuestionType.TrueFalse,
                "multiple" => TriviaQuestionType.MultipleChoice,
                _ => throw new ParseException("Unknown question type")
            };
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var questionType = (TriviaQuestionType)value;
            writer.WriteValue(questionType switch
            {
                TriviaQuestionType.TrueFalse => "boolean",
                TriviaQuestionType.MultipleChoice => "multiple",
                _ => throw new ArgumentException($"Didn't expect question type {questionType}")
            });
        }
    }
}