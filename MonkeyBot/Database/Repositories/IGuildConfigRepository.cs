using MonkeyBot.Database.Entities;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public interface IGuildConfigRepository : IRepository<GuildConfig>
    {
        GuildConfig GetOrCreate(ulong guildId);

        Task<GuildConfig> GetOrCreateAsync(ulong guildId);
    }
}