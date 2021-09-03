using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore.Internal;
using MonkeyBot.Common;
using MonkeyBot.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    /// <summary>Module that provides support for trivia game</summary>
    [Description("Trivia")]
    [MinPermissions(AccessLevel.User)]
    [RequireGuild]
    public class TriviaModule : BaseCommandModule
    {
        private readonly ITriviaService _triviaService;

        public TriviaModule(ITriviaService triviaService)
        {
            _triviaService = triviaService;
        }

        [Command("Trivia")]
        [Description("Starts a new trivia with the specified amount of questions.")]
        [Example("trivia 5")]
        public async Task StartTriviaAsync(CommandContext ctx, [Description("The number of questions to play.")] int questionAmount = 10)
        {
            bool success = await _triviaService.StartTriviaAsync(ctx.Guild.Id, ctx.Channel.Id, questionAmount);
            if (!success)
            {
                _ = await ctx.RespondAsync("Trivia could not be started :(");
            }
        }

        [Command("Stop")]
        [Description("Stops a running trivia")]
        public async Task StopTriviaAsync(CommandContext ctx)
        {
            if (!await (_triviaService?.StopTriviaAsync(ctx.Guild.Id, ctx.Channel.Id)))
            {
                _ = await ctx.ErrorAsync($"No trivia is running! Use {ctx.Prefix}trivia to create a new one.");
            }
        }

        [Command("TriviaScores")]
        [Description("Gets the global high scores")]
        [Example("triviascores 10")]
        public async Task GetScoresAsync(CommandContext ctx, [Description("The amount of scores to get.")] int amount = 5)
        {
            IEnumerable<(ulong userId, int score)> globalScores = await _triviaService.GetGlobalHighScoresAsync(ctx.Guild.Id, amount);
            if (globalScores != null && globalScores.Any())
            {

                string highScores = string.Join('\n', globalScores
                    .OrderByDescending(x => x.score)
                    .Select(async (x, i) => $"{i + 1}. {(await ctx.Guild.GetMemberAsync(x.userId))?.Mention ?? "Invalid user"}"));
                var embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(new DiscordColor(46, 191, 84))
                    .WithTitle("Trivia high scores")
                    .WithDescription(highScores);
                _ = await ctx.RespondAsync("", embed: embedBuilder.Build());
            }
            else
            {
                _ = await ctx.ErrorAsync("No stored scores found!");
            }
        }
    }
}