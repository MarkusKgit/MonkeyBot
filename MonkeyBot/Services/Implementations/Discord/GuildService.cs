using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database;
using MonkeyBot.Models;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class GuildService : IGuildService
    {
        private readonly MonkeyDBContext dbContext;

        public GuildService(MonkeyDBContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<GuildConfig> GetOrCreateConfigAsync(ulong guildId)
        {
            GuildConfig config = await dbContext.GuildConfigs.SingleOrDefaultAsync(c => c.GuildID == guildId).ConfigureAwait(false);
            if (config == null)
            {
                config = new GuildConfig { GuildID = guildId };
                _ = dbContext.GuildConfigs.Add(config);
                _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            return config;
        }        

        public Task UpdateConfigAsync(GuildConfig config)
        {
            _ = dbContext.GuildConfigs.Update(config);
            return dbContext.SaveChangesAsync();
        }

        public async Task RemoveConfigAsync(ulong guildId)
        {
            GuildConfig config = await dbContext.GuildConfigs.SingleOrDefaultAsync(c => c.GuildID == guildId).ConfigureAwait(false);
            if (config == null)
            {
                _ = dbContext.GuildConfigs.Remove(config);
                _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}
