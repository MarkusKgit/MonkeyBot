using MonkeyBot.Database.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public interface IRepository<TDb, TDTO> where TDb : BaseEntity where TDTO : class
    {
        Task<List<TDTO>> GetAllAsync();

        Task AddOrUpdateAsync(TDTO obj);

        Task RemoveAsync(TDTO obj);
    }

    public interface IRepository<TDb> where TDb : BaseEntity
    {
    }
}