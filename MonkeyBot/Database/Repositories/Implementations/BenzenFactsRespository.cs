using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public class BenzenFactsRespository : BaseRepository<BenzenFactEntity>, IBenzenFactsRespository
    {
        public BenzenFactsRespository(DbContext context) : base(context)
        {            
        }

        public async Task AddFactAsync(string fact)
        {
            await dbSet.AddAsync(new BenzenFactEntity() { Fact = fact });
        }

        public async Task<string> GetRandomFactAsync()
        {
            Random rnd = new Random();
            var allFacts = await dbSet.ToListAsync();
            return allFacts?.ElementAt(rnd.Next(0, allFacts.Count - 1)).Fact;
        }
    }
}