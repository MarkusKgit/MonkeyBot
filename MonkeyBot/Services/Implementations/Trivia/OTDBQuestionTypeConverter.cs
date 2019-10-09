using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonkeyBot.Services
{
    public class OTDBQuestionTypeConverter : JsonConverter<TriviaQuestionType>
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(string);

        public override TriviaQuestionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString() switch
            {
                "boolean" => TriviaQuestionType.TrueFalse,
                "multiple" => TriviaQuestionType.MultipleChoice,
                _ => throw new ParseException("Unknown question type")
            };
        }

        public override void Write(Utf8JsonWriter writer, TriviaQuestionType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value switch
            {
                TriviaQuestionType.TrueFalse => "boolean",
                TriviaQuestionType.MultipleChoice => "multiple",
                _ => throw new ArgumentException($"Didn't expect question type {value}")
            });
        }
    }
}