using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
using MonkeyBot.Services.Common.Trivia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    /// <summary>Module that provides support for trivia game</summary>
    [Group("Trivia")]
    [Name("Trivia")]
    [MinPermissions(AccessLevel.User)]
    [RequireContext(ContextType.Guild)]
    public class TriviaModule : ModuleBase
    {
        private ITriviaService triviaService; // The TriviaService will get injected in CommandHandler
        private CommandManager commandManager;
        private DbService db;

        public TriviaModule(IServiceProvider provider) // Create a constructor for the TriviaService dependency
        {
            triviaService = provider.GetService<ITriviaService>();
            commandManager = provider.GetService<CommandManager>();
            db = provider.GetService<DbService>();
        }

        [Command("Start")]
        [Remarks("Starts a new trivia with the specified amount of questions.")]
        public async Task StartTriviaAsync([Summary("The number of questions to play.")] int questionAmount = 10)
        {
            if (!await triviaService?.StartTriviaAsync(questionAmount, Context.Guild.Id, Context.Channel.Id))
                await ReplyAsync("Trivia could not be started :(");
        }

        [Command("Stop")]
        [Remarks("Stops a running trivia")]
        public async Task StopTriviaAsync()
        {
            if (!(await triviaService?.StopTriviaAsync(Context.Guild.Id, Context.Channel.Id)))
                await ReplyAsync($"No trivia is running! Use {commandManager.GetPrefixAsync(Context.Guild)}trivia start to create a new one.");
        }

        [Command("Skip")]
        [Remarks("Skips the current question")]
        public async Task SkipQuestionAsync()
        {
            if (!(await triviaService?.SkipQuestionAsync(Context.Guild.Id, Context.Channel.Id)))
                await ReplyAsync($"No trivia is running! Use {commandManager.GetPrefixAsync(Context.Guild)}trivia start to create a new one.");
        }

        [Command("Scores")]
        [Remarks("Gets the global scores")]
        public async Task GetScoresAsync([Summary("The amount of scores to get.")] int amount = 5)
        {
            string scores = await GetAllTimeHighScoresAsync(amount, Context.Guild.Id);
            if (!string.IsNullOrEmpty(scores))
                await ReplyAsync(scores);
        }

        private async Task<string> GetAllTimeHighScoresAsync(int count, ulong guildID)
        {
            List<TriviaScore> userScoresAllTime;
            using (var uow = db.UnitOfWork)
            {
                userScoresAllTime = (await uow.TriviaScores.GetGuildScoresAsync(guildID));
            }
            int correctedCount = Math.Min(count, userScoresAllTime.Count());
            if (userScoresAllTime == null || correctedCount < 1)
                return "No scores found!";
            var sortedScores = userScoresAllTime.OrderByDescending(x => x.Score);
            sortedScores.Take(correctedCount);
            List<string> scoresList = new List<string>();
            foreach (var score in sortedScores)
            {
                var userName = (await Context.Client.GetUserAsync(score.UserID)).Username;
                if (score.Score == 1)
                    scoresList.Add($"{userName}: 1 point");
                else
                    scoresList.Add($"{userName}: {score.Score} points");
            }
            string scores = $"**Top {correctedCount} of all time**:{Environment.NewLine}{string.Join(", ", scoresList)}";
            return scores;
        }
    }
}