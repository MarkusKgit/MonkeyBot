using MonkeyBot.Database.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public interface IRepository<TDb, TB> where TDb : BaseEntity where TB : class
    {
        Task<List<TB>> GetAllAsync();

        Task AddOrUpdateAsync(TB obj);

        Task RemoveAsync(TB obj);
    }

    public interface IRepository<TDb> where TDb : BaseEntity
    {
    }
}