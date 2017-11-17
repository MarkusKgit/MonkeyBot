using MonkeyBot.Database.Entities;
using MonkeyBot.Services.Common.SteamServerQuery;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public interface IGameServersRepository : IRepository<GuildConfigEntity, DiscordGameServerInfo>
    {
        Task<List<DiscordGameServerInfo>> GetAllForGuildAsync(ulong guildId);
    }
}