using MonkeyBot.Database.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public interface IRepository<TDb, TDTO> where TDb : BaseEntity where TDTO : class
    {
        Task<List<TDTO>> GetAllAsync(System.Linq.Expressions.Expression<System.Func<TDb, bool>> predicate = null);

        Task AddOrUpdateAsync(TDTO obj);

        Task RemoveAsync(TDTO obj);
    }
}