using MonkeyBot.Database.Entities;
using MonkeyBot.Modules.Common.Announcements;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public interface IAnnouncementRepository : IRepository<AnnouncementEntity>
    {
        Task<AnnouncementEntity> GetAsync(Announcement announcement);

        Task<AnnouncementEntity> AddOrUpdateAsync(Announcement announcement);
    }
}