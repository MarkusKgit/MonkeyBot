using MonkeyBot.Database.Entities;
using MonkeyBot.Services.Common.Feeds;

namespace MonkeyBot.Database.Repositories
{
    public interface IFeedsRepository : IRepository<FeedEntity, FeedDTO>
    {
    }
}