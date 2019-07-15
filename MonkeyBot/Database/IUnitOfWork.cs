using MonkeyBot.Database.Repositories;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Database
{
    public interface IUnitOfWork : IDisposable
    {        
        ITriviaScoresRepository TriviaScores { get; }

        IAnnouncementRepository Announcements { get; }

        IGameServersRepository GameServers { get; }

        IGameSubscriptionRepository GameSubscriptions { get; }

        IRoleButtonLinksRepository RoleButtonLinks { get; }

        Task<int> CompleteAsync();
    }
}