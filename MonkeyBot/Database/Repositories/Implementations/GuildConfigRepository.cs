using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public class GuildConfigRepository : BaseRepository<GuildConfigEntity>, IGuildConfigRepository
    {
        public GuildConfigRepository(DbContext context) : base(context)
        {
        }

        public async Task<GuildConfigEntity> GetOrCreateAsync(ulong guildId)
        {
            var config = await dbSet.FirstOrDefaultAsync(x => x.GuildId == guildId);
            if (config == null)
            {
                await dbSet.AddAsync(config = new GuildConfigEntity() { GuildId = guildId });
                await context.SaveChangesAsync();
            }
            return config;
        }
    }
}