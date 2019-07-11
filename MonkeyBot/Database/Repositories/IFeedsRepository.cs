using MonkeyBot.Database.Entities;
using MonkeyBot.Services;

namespace MonkeyBot.Database.Repositories
{
    public interface IFeedsRepository : IGuildRepository<FeedEntity, FeedDTO>
    {
    }
}