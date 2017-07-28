using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public class GuildConfigRepository : BaseRepository<GuildConfig>, IGuildConfigRepository
    {
        public GuildConfigRepository(DbContext context) : base(context)
        {
        }

        public GuildConfig GetOrCreate(ulong guildId)
        {
            var config = dbSet.FirstOrDefault(x => x.GuildId == guildId);
            if (config == null)
            {
                dbSet.Add(config = new GuildConfig());
                context.SaveChanges();
            }
            return config;
        }

        public async Task<GuildConfig> GetOrCreateAsync(ulong guildId)
        {
            var config = await dbSet.FirstOrDefaultAsync(x => x.GuildId == guildId);
            if (config == null)
            {
                await dbSet.AddAsync(config = new GuildConfig());
                await context.SaveChangesAsync();
            }
            return config;
        }
    }
}