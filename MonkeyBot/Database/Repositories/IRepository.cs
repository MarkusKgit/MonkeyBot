using MonkeyBot.Database.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public interface IRepository<TDb, TB> where TDb : BaseEntity where TB : class
    {
        Task<List<TB>> GetAllAsync();

        Task AddOrUpdateAsync(TB obj);

        //Task AddRangeAsync(params T[] objs);

        //void Remove(int id);

        //void Remove(T obj);

        //void RemoveRange(params T[] objs);

        //void Update(TB obj);

        //void UpdateRange(params T[] objs);
    }
}