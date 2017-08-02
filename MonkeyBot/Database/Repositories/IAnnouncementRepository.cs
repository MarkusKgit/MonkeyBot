using MonkeyBot.Database.Entities;
using MonkeyBot.Services.Common.Announcements;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public interface IAnnouncementRepository : IRepository<AnnouncementEntity, Announcement>
    {
        Task<Announcement> GetAnnouncementAsync(ulong guildId, ulong channelId, string announcementName);

        Task RemoveAsync(Announcement announcement);
    }
}