using dokas.FluentStrings;
using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using MonkeyBot.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public class AnnouncementRepository : BaseGuildRepository<AnnouncementEntity, Announcement>, IAnnouncementRepository
    {
        public AnnouncementRepository(DbContext context) : base(context)
        {
        }

        public override async Task<List<Announcement>> GetAllAsync(System.Linq.Expressions.Expression<System.Func<AnnouncementEntity, bool>> predicate = null)
        {
            var dbAnnouncements = await dbSet.ToListAsync().ConfigureAwait(false);
            if (dbAnnouncements == null)
                return null;
            List<Announcement> announcements = new List<Announcement>();
            foreach (var item in dbAnnouncements)
            {
                if (item.Type == AnnouncementType.Recurring && !item.CronExpression.IsEmpty())
                    announcements.Add(new RecurringAnnouncement(item.Name, item.CronExpression, item.Message, item.GuildId, item.ChannelId));
                if (item.Type == AnnouncementType.Single && item.ExecutionTime.HasValue)
                    announcements.Add(new SingleAnnouncement(item.Name, item.ExecutionTime.Value, item.Message, item.GuildId, item.ChannelId));
            }
            return announcements;
        }

        public override async Task AddOrUpdateAsync(Announcement announcement)
        {
            var dbAnnouncement = await GetDbAnnouncementAsync(announcement.GuildId, announcement.ChannelId, announcement.Name).ConfigureAwait(false);
            if (dbAnnouncement == null)
            {
                dbSet.Add(dbAnnouncement = new AnnouncementEntity
                {
                    Name = announcement.Name,
                    GuildId = announcement.GuildId,
                    ChannelId = announcement.ChannelId,
                    Message = announcement.Message,
                    Type = (announcement is RecurringAnnouncement) ? AnnouncementType.Recurring : AnnouncementType.Single,
                    CronExpression = (announcement as RecurringAnnouncement)?.CronExpression ?? string.Empty,
                    ExecutionTime = (announcement as SingleAnnouncement)?.ExcecutionTime
                });
            }
            else
            {
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
                dbSet.Update(dbAnnouncement);
            }
        }

        public override async Task RemoveAsync(Announcement obj)
        {
            if (obj == null)
                return;
            var entity = await GetDbAnnouncementAsync(obj.GuildId, obj.ChannelId, obj.Name).ConfigureAwait(false);
            if (entity != null)
                dbSet.Remove(entity);
        }

        private Task<AnnouncementEntity> GetDbAnnouncementAsync(ulong guildId, ulong channelId, string announcementName)
        {
            var dbAnnouncement = dbSet.FirstOrDefaultAsync(x => x.Name.Equals(announcementName, StringComparison.OrdinalIgnoreCase) && x.GuildId == guildId && x.ChannelId == channelId);
            return dbAnnouncement;
        }
    }
}