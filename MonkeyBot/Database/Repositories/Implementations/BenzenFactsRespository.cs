using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public class BenzenFactsRespository : BaseRepository<BenzenFactEntity, string>, IBenzenFactsRespository
    {
        public BenzenFactsRespository(DbContext context) : base(context)
        {
        }

        public async override Task AddOrUpdateAsync(string fact)
        {
            if (dbSet.Count(x => x.Fact == fact) < 1)
                await dbSet.AddAsync(new BenzenFactEntity { Fact = fact }).ConfigureAwait(false);
        }

        public async Task<string> GetRandomFactAsync()
        {
            Random rnd = new Random();
            var allFacts = await dbSet.ToListAsync().ConfigureAwait(false);
            return allFacts?.ElementAt(rnd.Next(0, allFacts.Count - 1)).Fact;
        }

        public async override Task RemoveAsync(string fact)
        {
            var entity = await dbSet.Where(x => x.Fact == fact).SingleOrDefaultAsync().ConfigureAwait(false);
            if (entity != null)
                dbSet.Remove(entity);
        }
    }
}