using MonkeyBot.Models;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IGuildService
    {
        Task<GuildConfig> GetOrCreateConfigAsync(ulong guildId);
        Task UpdateConfigAsync(GuildConfig config);
        Task RemoveConfigAsync(ulong guildId);
        Task<string> GetPrefixForGuild(ulong guildId);
    }
}
