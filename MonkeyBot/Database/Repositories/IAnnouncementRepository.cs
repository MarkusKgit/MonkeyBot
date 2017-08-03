using MonkeyBot.Database.Entities;
using MonkeyBot.Services.Common.Announcements;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public interface IAnnouncementRepository : IRepository<AnnouncementEntity, Announcement>
    {
        Task<Announcement> GetAsync(ulong guildId, ulong channelId, string announcementName);
    }
}