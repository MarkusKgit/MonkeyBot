using MonkeyBot.Database.Entities;
using MonkeyBot.Services.Common.SteamServerQuery;

namespace MonkeyBot.Database.Repositories
{
    public interface IGameServersRepository : IGuildRepository<GuildConfigEntity, DiscordGameServerInfo>
    {
    }
}