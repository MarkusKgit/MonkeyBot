using MonkeyBot.Database.Repositories;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Database
{
    public interface IUnitOfWork : IDisposable
    {
        IGuildConfigRepository GuildConfigs { get; }

        ITriviaScoresRepository TriviaScores { get; }

        IAnnouncementRepository Announcements { get; }

        Task<int> CompleteAsync();
    }
}