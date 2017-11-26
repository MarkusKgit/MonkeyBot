using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using MonkeyBot.Services;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    public class BenzenFactModule : ModuleBase
    {
        private DbService db;

        public BenzenFactModule(IServiceProvider provider)
        {
            db = provider.GetService<DbService>();
        }

        [Command("Benzen")]
        [Remarks("Returns a random fact about Benzen")]
        public async Task GetBenzenFactAsync()
        {
            using (var uow = db.UnitOfWork)
            {
                var fact = await uow.BenzenFacts.GetRandomFactAsync();
                if (!string.IsNullOrEmpty(fact))
                    await ReplyAsync(fact);
            }
        }

        [Command("AddBenzenFact")]
        [Remarks("Add a fact about Benzen")]
        public async Task AddBenzenFactAsync([Remainder] string fact)
        {
            fact = fact.Trim('\"').Trim();
            if (string.IsNullOrEmpty(fact))
            {
                await ReplyAsync("Please provide a fact!");
                return;
            }
            if (!fact.ToLower().Contains("benzen"))
            {
                await ReplyAsync("The fact must include Benzen!");
                return;
            }
            using (var uow = db.UnitOfWork)
            {
                await uow.BenzenFacts.AddFactAsync(fact);
                await uow.CompleteAsync();
            }
        }
    }
}