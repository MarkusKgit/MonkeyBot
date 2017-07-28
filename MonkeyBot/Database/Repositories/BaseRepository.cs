using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public class BaseRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected DbContext context;
        protected DbSet<T> dbSet;

        public BaseRepository(DbContext context)
        {
            this.context = context;
            dbSet = context.Set<T>();
        }

        public void Add(T obj) =>
            dbSet.Add(obj);

        public Task AddAsync(T obj) =>
            dbSet.AddAsync(obj);

        public void AddRange(params T[] objs) =>
            dbSet.AddRange(objs);

        public Task AddRangeAsync(params T[] objs) =>
            dbSet.AddRangeAsync(objs);

        public T Get(int id) =>
            dbSet.FirstOrDefault(e => e.Id == id);

        public Task<T> GetAsync(int id) =>
            dbSet.FirstOrDefaultAsync(e => e.Id == id);

        public IEnumerable<T> GetAll() =>
            dbSet.ToList();

        public Task<List<T>> GetAllAsync() =>
            dbSet.ToListAsync();

        public void Remove(int id) =>
            dbSet.Remove(this.Get(id));

        public void Remove(T obj) =>
            dbSet.Remove(obj);

        public void RemoveRange(params T[] objs) =>
            dbSet.RemoveRange(objs);

        public void Update(T obj) =>
            dbSet.Update(obj);

        public void UpdateRange(params T[] objs) =>
            dbSet.UpdateRange(objs);
    }
}