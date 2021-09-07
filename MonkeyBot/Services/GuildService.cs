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
                _dbContext.GuildConfigs.Add(config);
                await _dbContext.SaveChangesAsync();
            }
            return config;
        }        

        public Task UpdateConfigAsync(GuildConfig config)
        {
            _dbContext.GuildConfigs.Update(config);
            return _dbContext.SaveChangesAsync();
        }

        public async Task RemoveConfigAsync(ulong guildId)
        {
            GuildConfig config = await _dbContext.GuildConfigs.SingleOrDefaultAsync(c => c.GuildID == guildId);
            if (config == null)
            {
                _dbContext.GuildConfigs.Remove(config);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<string> GetPrefixForGuild(ulong guildId)
        {
            var guildConfig = await GetOrCreateConfigAsync(guildId);
            return guildConfig.CommandPrefix;
        }
    }
}
