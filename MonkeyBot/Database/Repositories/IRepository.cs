using MonkeyBot.Database.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<T> GetAsync(int id);

        Task<List<T>> GetAllAsync();

        Task AddAsync(T obj);

        Task AddRangeAsync(params T[] objs);

        void Remove(int id);

        void Remove(T obj);

        void RemoveRange(params T[] objs);

        void Update(T obj);

        void UpdateRange(params T[] objs);
    }
}