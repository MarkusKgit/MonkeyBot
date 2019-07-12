using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using MonkeyBot.Services;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public class FeedsRepository : BaseGuildRepository<FeedEntity, FeedDTO>, IFeedsRepository
    {
        public FeedsRepository(DbContext context) : base(context)
        {
        }

        public override async Task AddOrUpdateAsync(FeedDTO feed)
        {
            var dbFeed = await GetDBFeedAsync(feed).ConfigureAwait(false);
            if (dbFeed == null)
            {
                dbSet.Add(dbFeed = new FeedEntity
                {
                    GuildId = feed.GuildId,
                    ChannelId = feed.ChannelId,
                    URL = feed.URL,
                    LastUpdate = feed.LastUpdate
                });
            }
            else
            {
                dbFeed.GuildId = feed.GuildId;
                dbFeed.ChannelId = feed.ChannelId;
                dbFeed.URL = feed.URL;
                dbFeed.LastUpdate = feed.LastUpdate;
                dbSet.Update(dbFeed);
            }
        }

        public override async Task RemoveAsync(FeedDTO feed)
        {
            var entity = await GetDBFeedAsync(feed).ConfigureAwait(false);
            if (entity != null)
                dbSet.Remove(entity);
        }

        private Task<FeedEntity> GetDBFeedAsync(FeedDTO feed)
        {
            var dbFeed = dbSet.FirstOrDefaultAsync(x => x.GuildId == feed.GuildId && x.ChannelId == feed.ChannelId && x.URL == feed.URL);
            return dbFeed;
        }
    }
}