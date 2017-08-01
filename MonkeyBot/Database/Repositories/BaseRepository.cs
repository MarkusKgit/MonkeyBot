using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace MonkeyBot.Database.Repositories
{
    public abstract class BaseRepository<TDb, TB> : IRepository<TDb, TB> where TDb : BaseEntity where TB : class
    {
        protected DbContext context;
        protected DbSet<TDb> dbSet;

        public BaseRepository(DbContext context)
        {
            this.context = context;
            dbSet = context.Set<TDb>();
        }

        public virtual Task<List<TB>> GetAllAsync() =>
            dbSet.Select(x => AutoMapper.Mapper.Map<TB>(x)).ToListAsync();
        
        public abstract Task AddOrUpdateAsync(TB obj);


        //public Task AddAsync(T obj) =>
        //    dbSet.AddAsync(obj);

        //public Task AddRangeAsync(params T[] objs) =>
        //    dbSet.AddRangeAsync(objs);

        //public Task<T> GetAsync(int id) =>
        //    dbSet.FirstOrDefaultAsync(e => e.Id == id);

        //public IEnumerable<T> GetAll() =>
        //    dbSet.ToList();

        //public Task<List<T>> GetAllAsync() =>
        //    dbSet.ToListAsync();

        //public void Remove(int id) =>
        //    dbSet.Remove(dbSet.FirstOrDefault(e => e.Id == id));

        //public void Remove(T obj) =>
        //    dbSet.Remove(obj);

        //public void RemoveRange(params T[] objs) =>
        //    dbSet.RemoveRange(objs);

        //public void Update(T obj) =>
        //    dbSet.Update(obj);

        //public void UpdateRange(params T[] objs) =>
        //    dbSet.UpdateRange(objs);
    }
}