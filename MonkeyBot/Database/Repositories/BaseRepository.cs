using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public abstract class BaseRepository<TDb, TDTO> : IRepository<TDb, TDTO> where TDb : BaseEntity where TDTO : class
    {
        protected DbContext context;
        protected DbSet<TDb> dbSet;

        protected BaseRepository(DbContext context)
        {
            this.context = context;
            dbSet = context.Set<TDb>();
        }

        public virtual Task<List<TDTO>> GetAllAsync(System.Linq.Expressions.Expression<System.Func<TDb, bool>> predicate = null) =>
            dbSet.Where(predicate ?? (x => true)).Select(x => AutoMapper.Mapper.Map<TDTO>(x)).ToListAsync();

        public abstract Task AddOrUpdateAsync(TDTO obj);

        public abstract Task RemoveAsync(TDTO obj);
    }
}