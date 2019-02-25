using Discord.Commands;
using dokas.FluentStrings;
using MonkeyBot.Common;
using MonkeyBot.Services;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [Name("Benzen Facts")]
    public class BenzenFactModule : MonkeyModuleBase
    {
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
                var fact = await uow.BenzenFacts.GetRandomFactAsync();
                if (!fact.IsEmpty())
                    await ReplyAsync(fact);
            }
        }

        [Command("AddBenzenFact")]
        [Remarks("Add a fact about Benzen")]
        public async Task AddBenzenFactAsync([Remainder] string fact)
        {
            fact = fact.Trim('\"').Trim();
            if (fact.IsEmpty())
            {
                await ReplyAsync("Please provide a fact!");
                return;
            }
            if (!fact.ToLower().Contains("benzen"))
            {
                await ReplyAsync("The fact must include Benzen!");
                return;
            }
            using (var uow = dbService.UnitOfWork)
            {
                await uow.BenzenFacts.AddOrUpdateAsync(fact);
                await uow.CompleteAsync();
            }
            await ReplyAsync("Fact added");
        }
    }
}