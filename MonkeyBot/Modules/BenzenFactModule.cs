using Discord.Commands;
using MonkeyBot.Common;
using MonkeyBot.Database;
using MonkeyBot.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [Name("Benzen Facts")]
    public class BenzenFactModule : MonkeyModuleBase
    {
        private const string name = "benzen";
        private readonly MonkeyDBContext dbContext;

        public BenzenFactModule(MonkeyDBContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [Command("Benzen")]
        [Remarks("Returns a random fact about Benzen")]
        public async Task GetBenzenFactAsync()
        {
            var fact = dbContext.BenzenFacts.OrderBy(r => Guid.NewGuid()).FirstOrDefault()?.Fact;
            if (!fact.IsEmpty())
                await ReplyAsync(fact).ConfigureAwait(false);
        }

        [Command("AddBenzenFact")]
        [Remarks("Add a fact about Benzen")]
        public async Task AddBenzenFactAsync([Remainder] string fact)
        {
            fact = fact.Trim('\"').Trim();
            if (fact.IsEmpty())
            {
                await ReplyAsync("Please provide a fact!").ConfigureAwait(false);
                return;
            }
            if (!fact.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                await ReplyAsync("The fact must include Benzen!").ConfigureAwait(false);
                return;
            }
            if (dbContext.BenzenFacts.Any(f => f.Fact == fact))
            {
                await ReplyAsync("I already know this fact!").ConfigureAwait(false);
                return;
            }
            dbContext.BenzenFacts.Add(new BenzenFact(fact));
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
            await ReplyAsync("Fact added").ConfigureAwait(false);
        }
    }
}