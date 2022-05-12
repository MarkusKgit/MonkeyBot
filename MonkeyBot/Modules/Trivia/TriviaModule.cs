using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
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
                await ctx.RespondAsync("Trivia could not be started :(");
            }
        }

        [Command("Stop")]
        [Description("Stops a running trivia")]
        public async Task StopTriviaAsync(CommandContext ctx)
        {
            if (!await (_triviaService?.StopTriviaAsync(ctx.Guild.Id, ctx.Channel.Id)))
            {
                await ctx.ErrorAsync($"No trivia is running! Use {ctx.Prefix}trivia to create a new one.");
            }
        }

        [Command("TriviaScores")]
        [Description("Gets the global high scores")]
        [Example("triviascores 10")]
        public async Task GetScoresAsync(CommandContext ctx, [Description("The amount of scores to get.")] int amount = 5)
        {
            List<(ulong UserId, int Score)> globalScores = (await _triviaService.GetGlobalHighScoresAsync(ctx.Guild.Id, amount)).ToList();
            if (globalScores != null && globalScores.Any())
            {   
                List<string> scores = new List<string>();
                for (int i = 0; i < globalScores.Count; i++)
                {
                    var score = globalScores[i];
                    DiscordMember member = await ctx.Guild.GetMemberAsync(score.UserId);
                    scores.Add(Formatter.Bold($"#{i + 1} ") + (member?.DisplayName ?? "Invalid User") + $": {score.Score} points");
                }
                
                var embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(new DiscordColor(46, 191, 84))
                    .WithTitle("Trivia high scores")
                    .WithDescription(string.Join("\n", scores));
                await ctx.RespondAsync("", embed: embedBuilder.Build());
            }
            else
            {
                await ctx.ErrorAsync("No stored scores found!");
            }
        }
    }
}