using Newtonsoft.Json;
using System.Collections.Generic;

namespace MonkeyBot.Modules.Common.Trivia
{
    /// <summary>
    /// Question type as used by Open trivia database
    /// https://opentdb.com
    /// </summary>
    public class OTDBQuestion : ITriviaQuestion
    {
        [JsonProperty(PropertyName = "category")]
        public string Category { get; set; }

        [JsonProperty(PropertyName = "type")]
        [JsonConverter(typeof(OTDBQuestionTypeConverter))]
        public TriviaQuestionType Type { get; set; }

        [JsonProperty(PropertyName = "difficulty")]
        [JsonConverter(typeof(OTDBDifficultyConverter))]
        public TriviaQuestionDifficulty Difficulty { get; set; }

        [JsonProperty(PropertyName = "question")]
        public string Question { get; set; }

        [JsonProperty(PropertyName = "correct_answer")]
        public string CorrectAnswer { get; set; }

        [JsonProperty(PropertyName = "incorrect_answers")]
        public List<string> IncorrectAnswers { get; set; }
    }
}