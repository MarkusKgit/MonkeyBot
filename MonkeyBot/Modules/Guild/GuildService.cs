using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database;
using MonkeyBot.Models;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class GuildService : IGuildService
    {
        private readonly MonkeyDBContext _dbContext;

        public GuildService(MonkeyDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<GuildConfig> GetOrCreateConfigAsync(ulong guildId)
        {
            GuildConfig config = await _dbContext.GuildConfigs.SingleOrDefaultAsync(c => c.GuildID == guildId);
            if (config == null)
            {
                config = new GuildConfig { GuildID = guildId };
                _ = _dbContext.GuildConfigs.Add(config);
                _ = await _dbContext.SaveChangesAsync();
            }
            return config;
        }        

        public Task UpdateConfigAsync(GuildConfig config)
        {
            _ = _dbContext.GuildConfigs.Update(config);
            return _dbContext.SaveChangesAsync();
        }

        public async Task RemoveConfigAsync(ulong guildId)
        {
            GuildConfig config = await _dbContext.GuildConfigs.SingleOrDefaultAsync(c => c.GuildID == guildId);
            if (config == null)
            {
                _ = _dbContext.GuildConfigs.Remove(config);
                _ = await _dbContext.SaveChangesAsync();
            }
        }
    }
}
