using MonkeyBot.Database.Entities;
using MonkeyBot.Services;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public interface IAnnouncementRepository : IGuildRepository<AnnouncementEntity, Announcement>
    {
    }
}