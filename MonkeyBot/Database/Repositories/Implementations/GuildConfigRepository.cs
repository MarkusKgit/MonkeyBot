using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MonkeyBot.Common;
using MonkeyBot.Database.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public class GuildConfigRepository : BaseRepository<GuildConfigEntity, GuildConfig>, IGuildConfigRepository
    {
        public GuildConfigRepository(DbContext context) : base(context)
        {
        }

        public async Task<GuildConfig> GetAsync(ulong guildId)
        {
            var dbConfig = await dbSet.FirstOrDefaultAsync(x => (ulong)x.GuildId == guildId);
            if (dbConfig == null)
                return null;
            return Mapper.Map<GuildConfig>(dbConfig);
        }

        public override async Task AddOrUpdateAsync(GuildConfig obj)
        {
            var dbCfg = await dbSet.FirstOrDefaultAsync(x => (ulong)x.GuildId == obj.GuildId);
            if (dbCfg == null)
            {
                dbSet.Add(dbCfg = new GuildConfigEntity()
                {
                    GuildId = (long)obj.GuildId,
                    Rules = obj.Rules,
                    CommandPrefix = obj.CommandPrefix,
                    WelcomeMessageText = obj.WelcomeMessageText,
                    ListenToFeeds = obj.ListenToFeeds,
                    FeedUrls = obj.FeedUrls,
                    FeedChannelId = (long)obj.FeedChannelId
                });
            }
            else
            {
                dbCfg.GuildId = (long)obj.GuildId;
                dbCfg.Rules = new List<string>(obj.Rules);
                dbCfg.CommandPrefix = obj.CommandPrefix;
                dbCfg.WelcomeMessageText = obj.WelcomeMessageText;
                dbCfg.ListenToFeeds = obj.ListenToFeeds;
                dbCfg.FeedUrls = obj.FeedUrls;
                dbCfg.FeedChannelId = (long)obj.FeedChannelId;
                dbSet.Update(dbCfg);
            }
        }

        public override async Task RemoveAsync(GuildConfig obj)
        {
            if (obj == null)
                return;
            var entity = await dbSet.FirstOrDefaultAsync(x => (ulong)x.GuildId == obj.GuildId);
            if (entity != null)
                dbSet.Remove(entity);
        }
    }
}