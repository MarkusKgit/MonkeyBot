using MonkeyBot.Database.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public interface IGuildRepository<TDb, TDTO> : IRepository<TDb, TDTO> where TDb : BaseGuildEntity where TDTO : class
    {
        Task<List<TDTO>> GetAllForGuildAsync(ulong guildId);
    }
}