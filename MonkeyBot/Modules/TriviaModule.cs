using Discord.Commands;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
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

        public TriviaModule(ITriviaService triviaService) // Create a constructor for the TriviaService dependency
        {
            this.triviaService = triviaService;
        }

        [Command("Start")]
        [Remarks("Starts a new trivia with the specified amount of questions.")]
        public async Task StartTriviaAsync([Summary("The number of questions to play.")] int questionAmount = 10)
        {
            if (!await triviaService?.StartAsync(questionAmount, Context.Guild.Id, Context.Channel.Id))
                await ReplyAsync("Trivia could not be started :(");
        }

        [Command("Stop")]
        [Remarks("Stops a running trivia")]
        public async Task StopTriviaAsync()
        {
            if (!(await triviaService?.StopAsync(Context.Guild.Id, Context.Channel.Id)))
                await ReplyAsync($"No trivia is running! Use {(await Configuration.LoadAsync())?.Prefix}trivia start to create a new one.");
        }

        [Command("Skip")]
        [Remarks("Skips the current question")]
        public async Task SkipQuestionAsync()
        {
            if (!(await triviaService?.SkipQuestionAsync(Context.Guild.Id, Context.Channel.Id)))
                await ReplyAsync($"No trivia is running! Use {(await Configuration.LoadAsync())?.Prefix}trivia start to create a new one.");
        }

        [Command("Scores")]
        [Remarks("Gets the global scores")]
        public async Task GetScoresAsync([Summary("The amount of scores to get.")] int amount = 5)
        {
            string scores = await triviaService?.GetAllTimeHighScoresAsync(amount, Context.Guild.Id);
            if (!string.IsNullOrEmpty(scores))
                await ReplyAsync(scores);
        }
    }
}