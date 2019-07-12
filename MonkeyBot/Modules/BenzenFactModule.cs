using Discord.Commands;
using dokas.FluentStrings;
using MonkeyBot.Common;
using MonkeyBot.Services;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [Name("Benzen Facts")]
    public class BenzenFactModule : MonkeyModuleBase
    {
        private const string name = "benzen";
        private readonly DbService dbService;

        public BenzenFactModule(DbService db)
        {
            dbService = db;
        }

        [Command("Benzen")]
        [Remarks("Returns a random fact about Benzen")]
        public async Task GetBenzenFactAsync()
        {
            using (var uow = dbService.UnitOfWork)
            {
                var fact = await uow.BenzenFacts.GetRandomFactAsync().ConfigureAwait(false);
                if (!fact.IsEmpty())
                    await ReplyAsync(fact).ConfigureAwait(false);
            }
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
            using (var uow = dbService.UnitOfWork)
            {
                await uow.BenzenFacts.AddOrUpdateAsync(fact).ConfigureAwait(false);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await ReplyAsync("Fact added").ConfigureAwait(false);
        }
    }
}