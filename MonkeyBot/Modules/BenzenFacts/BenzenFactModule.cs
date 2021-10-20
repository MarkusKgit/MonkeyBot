using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MonkeyBot.Services;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [Description("Benzen Facts")]
    public class BenzenFactModule : BaseCommandModule
    {
        private readonly IBenzenFactService _benzenFactService;

        public BenzenFactModule(IBenzenFactService benzenFactService)
        {
            _benzenFactService = benzenFactService;
        }

        [Command("Benzen")]
        [Description("Returns a random fact about Benzen")]
        public async Task GetBenzenFactAsync(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();
            (string fact, int factNumber) = await _benzenFactService.GetRandomFactAsync();
            if (!fact.IsEmpty())
            {
                DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.DarkBlue)
                    .WithTitle($"Benzen Fact #{factNumber}")
                    .WithDescription(fact);
                await ctx.RespondAsync(builder.Build());
            }
        }

        [Command("AddBenzenFact")]
        [Description("Add a fact about Benzen")]
        public async Task AddBenzenFactAsync(CommandContext ctx, [RemainingText, Description("The fact you want to add")] string fact)
        {
            fact = fact.Trim('\"').Trim();
            if (fact.IsEmpty())
            {
                await ctx.ErrorAsync("Please provide a fact!");
                return;
            }
            if (!fact.Contains("benzen", StringComparison.OrdinalIgnoreCase))
            {
                await ctx.ErrorAsync("The fact must include Benzen!");
                return;
            }
            if (await _benzenFactService.Exists(fact))
            {
                await ctx.ErrorAsync("I already know this fact!");
                return;
            }
            await _benzenFactService.AddFactAsync(fact);
            await ctx.OkAsync("Fact added");
        }
    }
}