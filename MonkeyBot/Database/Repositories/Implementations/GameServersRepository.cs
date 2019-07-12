using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using MonkeyBot.Services;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public class GameServersRepository : BaseGuildRepository<GameServerEntity, DiscordGameServerInfo>, IGameServersRepository
    {
        public GameServersRepository(DbContext context) : base(context)
        {
        }

        public override async Task AddOrUpdateAsync(DiscordGameServerInfo obj)
        {
            var dbServerInfo = await dbSet.FirstOrDefaultAsync(x => x.GuildId == obj.GuildId && x.ChannelId == obj.ChannelId && x.IP.Address.ToString() == obj.IP.Address.ToString() && x.IP.Port == obj.IP.Port).ConfigureAwait(false);
            if (dbServerInfo == null)
            {
                dbSet.Add(dbServerInfo = new GameServerEntity
                {
                    GameServerType = obj.GameServerType.ToString(),
                    GuildId = obj.GuildId,
                    ChannelId = obj.ChannelId,
                    IP = obj.IP,
                    MessageId = obj.MessageId,
                    GameVersion = obj.GameVersion,
                    LastVersionUpdate = obj.LastVersionUpdate
                });
            }
            else
            {
                dbServerInfo.GameServerType = obj.GameServerType.ToString();
                dbServerInfo.GuildId = obj.GuildId;
                dbServerInfo.ChannelId = obj.ChannelId;
                dbServerInfo.IP = obj.IP;
                dbServerInfo.MessageId = obj.MessageId;
                dbServerInfo.GameVersion = obj.GameVersion;
                dbServerInfo.LastVersionUpdate = obj.LastVersionUpdate;
            }
        }

        public override async Task RemoveAsync(DiscordGameServerInfo obj)
        {
            if (obj == null)
                return;
            var entity = await dbSet.FirstOrDefaultAsync(x => x.GuildId == obj.GuildId && x.ChannelId == obj.ChannelId && x.IP.Address.ToString() == obj.IP.Address.ToString() && x.IP.Port == obj.IP.Port).ConfigureAwait(false);
            if (entity != null)
                dbSet.Remove(entity);
        }
    }
}