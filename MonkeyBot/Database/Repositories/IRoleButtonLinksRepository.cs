using MonkeyBot.Database.Entities;
using MonkeyBot.Services.Common.RoleButtons;

namespace MonkeyBot.Database.Repositories
{
    public interface IRoleButtonLinksRepository : IGuildRepository<RoleButtonLinkEntity, RoleButtonLink>
    {
    }
}