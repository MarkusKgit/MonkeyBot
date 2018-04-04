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

        IFeedsRepository Feeds { get; }

        IBenzenFactsRespository BenzenFacts { get; }

        IGameServersRepository GameServers { get; }

        IGameSubscriptionRepository GameSubscriptions { get; }

        Task<int> CompleteAsync();
    }
}