using MonkeyBot.Database.Entities;
using MonkeyBot.Services.Common.GameSubscription;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public interface IGameSubscriptionRepository : IRepository<GameSubscriptionEntity, GameSubscription>
    {
        Task<List<GameSubscription>> GetAllForUserAsync(ulong userId);
    }
}