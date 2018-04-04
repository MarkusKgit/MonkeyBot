using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public abstract class BaseGuildRepository<TDb, TDTO> : BaseRepository<TDb, TDTO>, IGuildRepository<TDb, TDTO> where TDb : BaseGuildEntity where TDTO : class
    {
        protected BaseGuildRepository(DbContext context) : base(context)
        {
        }

        public Task<List<TDTO>> GetAllForGuildAsync(ulong guildId) =>
            dbSet.Where(x => x.GuildId == guildId).Select(x => AutoMapper.Mapper.Map<TDTO>(x)).ToListAsync();
    }
}