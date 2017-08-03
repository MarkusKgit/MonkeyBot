using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public abstract Task RemoveAsync(TB obj);
    }
}