using System.Collections.Generic;

namespace MonkeyBot.Modules.Common.Trivia
{
    public interface ITriviaQuestion
    {
        string Category { get; set; }

        TriviaQuestionType Type { get; set; }

        TriviaQuestionDifficulty Difficulty { get; set; }

        string Question { get; set; }

        string CorrectAnswer { get; set; }

        List<string> IncorrectAnswers { get; set; }
    }
}