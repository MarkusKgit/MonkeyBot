using MonkeyBot.Common;
using MonkeyBot.Database.Entities;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public interface IGuildConfigRepository : IRepository<GuildConfigEntity, GuildConfig>
    {
        Task<GuildConfig> GetAsync(ulong guildId);
    }
}