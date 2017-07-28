using MonkeyBot.Database.Entities;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public interface IGuildConfigRepository : IRepository<GuildConfigEntity>
    {
        GuildConfigEntity GetOrCreate(ulong guildId);

        Task<GuildConfigEntity> GetOrCreateAsync(ulong guildId);
    }
}