using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public class GuildConfigRepository : BaseRepository<GuildConfigEntity>, IGuildConfigRepository
    {
        public GuildConfigRepository(DbContext context) : base(context)
        {
        }

        public GuildConfigEntity GetOrCreate(ulong guildId)
        {
            var config = dbSet.FirstOrDefault(x => x.GuildId == guildId);
            if (config == null)
            {
                dbSet.Add(config = new GuildConfigEntity());
                context.SaveChanges();
            }
            return config;
        }

        public async Task<GuildConfigEntity> GetOrCreateAsync(ulong guildId)
        {
            var config = await dbSet.FirstOrDefaultAsync(x => x.GuildId == guildId);
            if (config == null)
            {
                await dbSet.AddAsync(config = new GuildConfigEntity());
                await context.SaveChangesAsync();
            }
            return config;
        }
    }
}