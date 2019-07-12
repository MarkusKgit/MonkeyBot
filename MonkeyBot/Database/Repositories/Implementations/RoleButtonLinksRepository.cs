using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using MonkeyBot.Services;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public class RoleButtonLinksRepository : BaseGuildRepository<RoleButtonLinkEntity, RoleButtonLink>, IRoleButtonLinksRepository
    {
        public RoleButtonLinksRepository(DbContext context) : base(context)
        {
        }

        public async override Task AddOrUpdateAsync(RoleButtonLink obj)
        {
            var link = await dbSet.FirstOrDefaultAsync(x => x.GuildId == obj.GuildId && x.MessageId == obj.MessageId && x.RoleId == obj.RoleId && x.EmoteString == obj.EmoteString).ConfigureAwait(false);
            if (link == null)
            {
                dbSet.Add(link = new RoleButtonLinkEntity
                {
                    GuildId = obj.GuildId,
                    MessageId = obj.MessageId,
                    RoleId = obj.RoleId,
                    EmoteString = obj.EmoteString
                });
            }
            else
            {
                link.GuildId = obj.GuildId;
                link.MessageId = obj.MessageId;
                link.RoleId = obj.RoleId;
                link.EmoteString = obj.EmoteString;
            }
        }

        public async override Task RemoveAsync(RoleButtonLink obj)
        {
            if (obj == null)
                return;
            var link = await dbSet.FirstOrDefaultAsync(x => x.GuildId == obj.GuildId && x.MessageId == obj.MessageId && x.RoleId == obj.RoleId && x.EmoteString == obj.EmoteString).ConfigureAwait(false);
            if (link != null)
                dbSet.Remove(link);
        }
    }
}