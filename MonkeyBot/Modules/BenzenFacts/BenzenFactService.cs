using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database;
using MonkeyBot.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class BenzenFactService : IBenzenFactService
    {
        private readonly IDbContextFactory<MonkeyDBContext> _dbContextFactory;

        public BenzenFactService(IDbContextFactory<MonkeyDBContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public Task<(string Fact, int Number)> GetRandomFactAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            int totalFacts = dbContext.BenzenFacts.Count();
            var r = new Random();
            int randomOffset = r.Next(0, totalFacts);

            string fact = dbContext
                .BenzenFacts
                .AsQueryable()
                .OrderBy(x => x.ID)
                .Skip(randomOffset)
                .FirstOrDefault()?
                .Fact;
            return Task.FromResult((fact, randomOffset + 1));
        }

        public async Task AddFactAsync(string fact)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            dbContext.BenzenFacts.Add(new BenzenFact(fact));
            await dbContext.SaveChangesAsync();
        }

        public Task<bool> Exists(string fact)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return Task.FromResult(
                           dbContext
                           .BenzenFacts
                           .AsEnumerable()
                           .Any(f => string.Equals(f.Fact, fact, StringComparison.Ordinal))
                           );
        }
    }
}
