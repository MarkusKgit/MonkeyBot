using MonkeyBot.Database.Entities;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public interface IGuildConfigRepository : IRepository<GuildConfigEntity>
    {
        Task<GuildConfigEntity> GetOrCreateAsync(ulong guildId);
    }
}