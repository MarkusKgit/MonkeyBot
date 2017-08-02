using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using MonkeyBot.Services.Common.Announcements;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace MonkeyBot.Database.Repositories
{
    public class AnnouncementRepository : BaseRepository<AnnouncementEntity, Announcement>, IAnnouncementRepository
    {
        public AnnouncementRepository(DbContext context) : base(context)
        {
        }

        public override async Task<List<Announcement>> GetAllAsync()
        {
            var dbAnnouncements = await dbSet.ToListAsync();
            if (dbAnnouncements == null)
                return null;
            List<Announcement> announcements = new List<Announcement>();
            foreach (var item in dbAnnouncements)
            {
                if (item.Type == AnnouncementType.Recurring && !string.IsNullOrEmpty(item.CronExpression))
                    announcements.Add(new RecurringAnnouncement(item.Name, item.CronExpression, item.Message, item.GuildId, item.ChannelId));
                if (item.Type == AnnouncementType.Single && item.ExecutionTime.HasValue)
                    announcements.Add(new SingleAnnouncement(item.Name, item.ExecutionTime.Value, item.Message, item.GuildId, item.ChannelId));
            }
            return announcements;
        }

        public async Task<Announcement> GetAnnouncementAsync(ulong guildId, ulong channelId, string announcementName)
        {
            var dbAnnouncement = await GetDbAnnouncementAsync(guildId, channelId, announcementName);
            if (dbAnnouncement == null)
                return null;
            if (dbAnnouncement.Type == AnnouncementType.Recurring)
            {
                return new RecurringAnnouncement()
                {
                    Name = dbAnnouncement.Name,
                    GuildId = dbAnnouncement.GuildId,
                    ChannelId = dbAnnouncement.ChannelId,
                    CronExpression = dbAnnouncement.CronExpression,
                    Message = dbAnnouncement.Message
                };
            }
            else if (dbAnnouncement.Type == AnnouncementType.Single)
            {
                return new SingleAnnouncement()
                {
                    Name = dbAnnouncement.Name,
                    GuildId = dbAnnouncement.GuildId,
                    ChannelId = dbAnnouncement.ChannelId,
                    ExcecutionTime = dbAnnouncement.ExecutionTime.Value,
                    Message = dbAnnouncement.Message
                };
            }
            else
                return null;
        }

        public override async Task AddOrUpdateAsync(Announcement announcement)
        {
            var dbAnnouncement = await GetDbAnnouncementAsync(announcement.GuildId, announcement.ChannelId, announcement.Name);
            if (dbAnnouncement == null)
            {
                dbSet.Add(dbAnnouncement = new AnnouncementEntity()
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

        public async Task RemoveAsync(Announcement announcement)
        {
            var entity = await GetDbAnnouncementAsync(announcement.GuildId, announcement.ChannelId, announcement.Name);
            if (entity != null)
                dbSet.Remove(entity);
        }

        private Task<AnnouncementEntity> GetDbAnnouncementAsync(ulong guildId, ulong channelId, string announcementName)
        {
            var dbAnnouncement = dbSet.FirstOrDefaultAsync(x => x.Name == announcementName && x.GuildId == guildId && x.ChannelId == channelId);
            return dbAnnouncement;
        }                
    }
}