using MonkeyBot.Database.Entities;
using MonkeyBot.Services.Common;

namespace MonkeyBot.Database.Repositories
{
    public interface IGameServersRepository : IGuildRepository<GameServerEntity, DiscordGameServerInfo>
    {
    }
}