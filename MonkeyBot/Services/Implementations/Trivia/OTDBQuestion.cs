using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MonkeyBot.Services
{
    /// <summary>
    /// Question type as used by Open trivia database
    /// https://opentdb.com
    /// </summary>
    public class OTDBQuestion : ITriviaQuestion
    {
        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("type")]
        [JsonConverter(typeof(OTDBQuestionTypeConverter))]
        public TriviaQuestionType Type { get; set; }

        [JsonPropertyName("difficulty")]
        [JsonConverter(typeof(OTDBDifficultyConverter))]
        public TriviaQuestionDifficulty Difficulty { get; set; }

        [JsonPropertyName("question")]
        public string Question { get; set; }

        [JsonPropertyName("correct_answer")]
        public string CorrectAnswer { get; set; }

        [JsonPropertyName("incorrect_answers")]
        public List<string> IncorrectAnswers { get; set; }
    }
}