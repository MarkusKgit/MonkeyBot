using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MonkeyBot.Database;
using MonkeyBot.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [Description("Benzen Facts")]
    public class BenzenFactModule : BaseCommandModule
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
            await ctx.TriggerTypingAsync();
            int totalFacts = dbContext.BenzenFacts.Count();
            var r = new Random();
            int randomOffset = r.Next(0, totalFacts);
            string fact = dbContext.BenzenFacts.AsQueryable().Skip(randomOffset).FirstOrDefault()?.Fact;
            if (!fact.IsEmpty())
            {
                DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.DarkBlue)
                    .WithTitle($"Benzen Fact #{randomOffset + 1}")
                    .WithDescription(fact);
                _ = await ctx.RespondAsync(builder.Build());
            }
        }

        [Command("AddBenzenFact")]
        [Description("Add a fact about Benzen")]
        public async Task AddBenzenFactAsync(CommandContext ctx, [RemainingText] string fact)
        {
            fact = fact.Trim('\"').Trim();
            if (fact.IsEmpty())
            {
                _ = await ctx.ErrorAsync("Please provide a fact!");
                return;
            }
            if (!fact.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                _ = await ctx.ErrorAsync("The fact must include Benzen!");
                return;
            }
            if (dbContext.BenzenFacts.Any(f => f.Fact == fact))
            {
                _ = await ctx.ErrorAsync("I already know this fact!");
                return;
            }
            _ = dbContext.BenzenFacts.Add(new BenzenFact(fact));
            _ = await dbContext.SaveChangesAsync();
            await ctx.OkAsync("Fact added");
        }
    }
}