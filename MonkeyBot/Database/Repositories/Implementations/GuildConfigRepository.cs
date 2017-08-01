using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MonkeyBot.Common;
using MonkeyBot.Database.Entities;
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
            var dbConfig = await dbSet.FirstOrDefaultAsync(x => x.GuildId == guildId);
            if (dbConfig == null)
                return null;
            return Mapper.Map<GuildConfig>(dbConfig);
        }
        
        public override async Task AddOrUpdateAsync(GuildConfig obj)
        {
            var dbCfg = await dbSet.FirstOrDefaultAsync(x => x.GuildId == obj.GuildId);
            if (dbCfg == null)
                dbSet.Add(dbCfg = new GuildConfigEntity());
            dbCfg.GuildId = obj.GuildId;
            dbCfg.Rules = obj.Rules;
            dbCfg.CommandPrefix = obj.CommandPrefix;
            dbCfg.WelcomeMessageText = obj.WelcomeMessageText;
            dbSet.Update(dbCfg);
        }

        
        
    }
}