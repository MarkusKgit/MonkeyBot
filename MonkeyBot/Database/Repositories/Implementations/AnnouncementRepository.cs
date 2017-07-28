using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using MonkeyBot.Modules.Common.Announcements;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public class AnnouncementRepository : BaseRepository<AnnouncementEntity>, IAnnouncementRepository
    {
        public AnnouncementRepository(DbContext context) : base(context)
        {
        }

        public Task<AnnouncementEntity> GetAsync(Announcement announcement)
        {
            var dbAnnouncement = dbSet.FirstOrDefaultAsync(x => x.Name == announcement.Name && x.GuildId == announcement.GuildId && x.ChannelId == announcement.ChannelId && x.Message == announcement.Message);
            return dbAnnouncement;
        }

        public async Task<AnnouncementEntity> AddOrUpdateAsync(Announcement announcement)
        {
            var dbAnnouncement = await GetAsync(announcement);
            if (dbAnnouncement == null)
                dbSet.Add(dbAnnouncement = new AnnouncementEntity());

            dbAnnouncement.Name = announcement.Name;
            dbAnnouncement.GuildId = announcement.GuildId;
            dbAnnouncement.ChannelId = announcement.ChannelId;
            dbAnnouncement.Message = announcement.Message;
            if (announcement is RecurringAnnouncement)
            {
                dbAnnouncement.CronExpression = (announcement as RecurringAnnouncement).CronExpression;
                dbAnnouncement.Type = AnnouncementType.Recurring;
            }
            else if (announcement is SingleAnnouncement)
            {
                dbAnnouncement.ExecutionTime = (announcement as SingleAnnouncement).ExcecutionTime;
                dbAnnouncement.Type = AnnouncementType.Single;
            }
            context.SaveChanges();
            return dbAnnouncement;
        }
    }
}