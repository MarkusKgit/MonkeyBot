using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database;
using MonkeyBot.Models;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class GuildService : IGuildService
    {
        private readonly IDbContextFactory<MonkeyDBContext> _dbContextFactory;

        public GuildService(IDbContextFactory<MonkeyDBContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<GuildConfig> GetOrCreateConfigAsync(ulong guildId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            GuildConfig config = await dbContext.GuildConfigs.SingleOrDefaultAsync(c => c.GuildID == guildId);
            if (config == null)
            {
                config = new GuildConfig { GuildID = guildId };
                dbContext.GuildConfigs.Add(config);
                await dbContext.SaveChangesAsync();
            }
            return config;
        }        

        public Task UpdateConfigAsync(GuildConfig config)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            dbContext.GuildConfigs.Update(config);
            return dbContext.SaveChangesAsync();
        }

        public async Task RemoveConfigAsync(ulong guildId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            GuildConfig config = await dbContext.GuildConfigs.SingleOrDefaultAsync(c => c.GuildID == guildId);
            if (config != null)
            {
                dbContext.GuildConfigs.Remove(config);
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task<string> GetPrefixForGuild(ulong guildId)
        {
            var guildConfig = await GetOrCreateConfigAsync(guildId);
            return guildConfig.CommandPrefix;
        }
    }
}
