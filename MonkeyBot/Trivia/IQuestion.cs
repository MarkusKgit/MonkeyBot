using System.Collections.Generic;

namespace MonkeyBot.Trivia
{
    public interface IQuestion
    {
        string Category { get; set; }

        QuestionType Type { get; set; }

        QuestionDifficulty Difficulty { get; set; }

        string Question { get; set; }

        string CorrectAnswer { get; set; }

        List<string> IncorrectAnswers { get; set; }
    }
}