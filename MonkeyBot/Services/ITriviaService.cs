using MonkeyBot.Trivia;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface ITriviaService
    {
        IDictionary<ulong, int> UserScoresAllTime { get; }

        IEnumerable<IQuestion> Questions { get; }

        TriviaStatus Status { get; }

        Task StartAsync(int questionsToPlay, ulong guildID, ulong channelID);

        Task SkipQuestionAsync();

        Task StopAsync();

        Task<string> GetAllTimeHighScoresAsync(int Count);

        Task LoadScoreAsync();

        Task SaveScoresAsync();
    }

    public enum TriviaStatus
    {
        Running,
        Stopped
    }
}