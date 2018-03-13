using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using MonkeyBot.Services.Common.SteamServerQuery;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public class GameServersRepository : BaseRepository<GameServerEntity, DiscordGameServerInfo>, IGameServersRepository
    {
        public GameServersRepository(DbContext context) : base(context)
        {
        }

        public override async Task AddOrUpdateAsync(DiscordGameServerInfo obj)
        {
            var dbServerInfo = await dbSet.FirstOrDefaultAsync(x => (ulong)x.GuildId == obj.GuildId && (ulong)x.ChannelId == obj.ChannelId && x.IP.Address.ToString() == obj.IP.Address.ToString() && x.IP.Port == obj.IP.Port);
            if (dbServerInfo == null)
            {
                dbSet.Add(dbServerInfo = new GameServerEntity
                {
                    GuildId = (long)obj.GuildId,
                    ChannelId = (long)obj.ChannelId,
                    IP = obj.IP,
                    MessageId = (long?)obj.MessageId,
                    GameVersion = obj.GameVersion,
                    LastVersionUpdate = obj.LastVersionUpdate
                });
            }
            else
            {
                dbServerInfo.GuildId = (long)obj.GuildId;
                dbServerInfo.ChannelId = (long)obj.ChannelId;
                dbServerInfo.IP = obj.IP;
                dbServerInfo.MessageId = (long?)obj.MessageId;
                dbServerInfo.GameVersion = obj.GameVersion;
                dbServerInfo.LastVersionUpdate = obj.LastVersionUpdate;
            }
        }

        public async Task<List<DiscordGameServerInfo>> GetAllForGuildAsync(ulong guildId)
        {
            var serverInfo = await dbSet.Where(x => (ulong)x.GuildId == guildId).ToListAsync();
            if (serverInfo == null)
                return null;
            return Mapper.Map<List<DiscordGameServerInfo>>(serverInfo);
        }

        public override async Task RemoveAsync(DiscordGameServerInfo obj)
        {
            if (obj == null)
                return;
            var entity = await dbSet.FirstOrDefaultAsync(x => (ulong)x.GuildId == obj.GuildId && (ulong)x.ChannelId == obj.ChannelId && x.IP.Address.ToString() == obj.IP.Address.ToString() && x.IP.Port == obj.IP.Port);
            if (entity != null)
                dbSet.Remove(entity);
        }
    }
}