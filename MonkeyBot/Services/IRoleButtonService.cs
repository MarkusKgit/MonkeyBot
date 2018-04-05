using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IRoleButtonService
    {
        Task AddRoleButtonLinkAsync(ulong guildId, ulong messageId, ulong roleId, string emoji);

        Task RemoveRoleButtonLinkAsync(ulong guildId, ulong messageId, ulong roleId);

        Task RemoveAllRoleButtonLinksAsync(ulong guildId);

        Task<string> ListAllAsync(ulong guildId);
    }
}