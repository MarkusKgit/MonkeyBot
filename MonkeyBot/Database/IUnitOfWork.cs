using MonkeyBot.Database.Repositories;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Database
{
    public interface IUnitOfWork : IDisposable
    {
        MonkeyDBContext context { get; }

        ITriviaScoresRepository TriviaScores { get; }

        int Complete();

        Task<int> CompleteAsync();
    }
}