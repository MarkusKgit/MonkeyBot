using MonkeyBot.Database.Entities;
using MonkeyBot.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public interface IGameSubscriptionRepository : IGuildRepository<GameSubscriptionEntity, GameSubscription>
    {
        Task<List<GameSubscription>> GetAllForUserAsync(ulong userId);
    }
}