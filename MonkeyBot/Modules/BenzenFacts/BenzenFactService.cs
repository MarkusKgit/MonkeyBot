using MonkeyBot.Database;
using MonkeyBot.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class BenzenFactService : IBenzenFactService
    {
        private readonly MonkeyDBContext _dbContext;

        public BenzenFactService(MonkeyDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<(string Fact, int Number)> GetRandomFactAsync()
        {
            int totalFacts = _dbContext.BenzenFacts.Count();
            var r = new Random();
            int randomOffset = r.Next(0, totalFacts);

            string fact = _dbContext
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
            _dbContext.BenzenFacts.Add(new BenzenFact(fact));
            await _dbContext.SaveChangesAsync();
        }

        public Task<bool> Exists(string fact)
        {
            return Task.FromResult(
                           _dbContext
                           .BenzenFacts
                           .AsEnumerable()
                           .Any(f => string.Equals(f.Fact, fact, StringComparison.Ordinal))
                           );
        }
    }
}
