using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using MonkeyBot.Common;
using MonkeyBot.Database;
using MonkeyBot.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [Description("Benzen Facts")]
    public class BenzenFactModule : MonkeyModuleBase
    {
        private const string name = "benzen";
        private readonly MonkeyDBContext dbContext;

        public BenzenFactModule(MonkeyDBContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [Command("Benzen")]
        [Description("Returns a random fact about Benzen")]
        public async Task GetBenzenFactAsync(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);
            int totalFacts = dbContext.BenzenFacts.Count();
            var r = new Random();
            int randomOffset = r.Next(0, totalFacts);
            string fact = dbContext.BenzenFacts.AsQueryable().Skip(randomOffset).FirstOrDefault()?.Fact;
            if (!fact.IsEmpty())
            {
                _ = await ctx.RespondAsync(fact).ConfigureAwait(false);
            }
        }

        [Command("AddBenzenFact")]
        [Description("Add a fact about Benzen")]
        public async Task AddBenzenFactAsync(CommandContext ctx, [RemainingText] string fact)
        {
            fact = fact.Trim('\"').Trim();
            if (fact.IsEmpty())
            {
                _ = await ctx.RespondAsync("Please provide a fact!").ConfigureAwait(false);
                return;
            }
            if (!fact.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                _ = await ctx.RespondAsync("The fact must include Benzen!").ConfigureAwait(false);
                return;
            }
            if (dbContext.BenzenFacts.Any(f => f.Fact == fact))
            {
                _ = await ctx.RespondAsync("I already know this fact!").ConfigureAwait(false);
                return;
            }
            _ = dbContext.BenzenFacts.Add(new BenzenFact(fact));
            _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
            _ = await ctx.RespondAsync("Fact added").ConfigureAwait(false);
        }
    }
}