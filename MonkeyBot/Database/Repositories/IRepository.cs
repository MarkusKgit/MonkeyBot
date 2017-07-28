using MonkeyBot.Database.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public interface IRepository<T> where T : BaseEntity
    {
        T Get(int id);

        Task<T> GetAsync(int id);

        IEnumerable<T> GetAll();

        Task<List<T>> GetAllAsync();

        void Add(T obj);

        Task AddAsync(T obj);

        void AddRange(params T[] objs);

        Task AddRangeAsync(params T[] objs);

        void Remove(int id);

        void Remove(T obj);

        void RemoveRange(params T[] objs);

        void Update(T obj);

        void UpdateRange(params T[] objs);
    }
}