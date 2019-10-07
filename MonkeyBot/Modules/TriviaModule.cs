using Discord;
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
    public class TriviaModule : MonkeyModuleBase
    {
        private readonly ITriviaService triviaService;
        private readonly CommandManager commandManager;

        public TriviaModule(ITriviaService triviaService, CommandManager commandManager)
        {
            this.triviaService = triviaService;
            this.commandManager = commandManager;
        }

        [Command("Start", RunMode = RunMode.Async)]
        [Remarks("Starts a new trivia with the specified amount of questions.")]
        [Example("!trivia start 5")]
        public async Task StartTriviaAsync([Summary("The number of questions to play.")] int questionAmount = 10)
        {
            bool success = await triviaService.StartTriviaAsync(questionAmount, Context as SocketCommandContext).ConfigureAwait(false);
            if (!success)
            {
                _ = await ReplyAsync("Trivia could not be started :(").ConfigureAwait(false);
            }
        }

        [Command("Stop")]
        [Remarks("Stops a running trivia")]
        public async Task StopTriviaAsync()
        {
            if (!await (triviaService?.StopTriviaAsync(new DiscordId(Context.Guild.Id, Context.Channel.Id, null))).ConfigureAwait(false))
            {
                _ = await ReplyAsync($"No trivia is running! Use {commandManager.GetPrefixAsync(Context.Guild)}trivia start to create a new one.").ConfigureAwait(false);
            }
        }

        [Command("Skip")]
        [Remarks("Skips the current question")]
        public async Task SkipQuestionAsync()
        {
            if (!await (triviaService?.SkipQuestionAsync(new DiscordId(Context.Guild.Id, Context.Channel.Id, null))).ConfigureAwait(false))
            {
                _ = await ReplyAsync($"No trivia is running! Use {commandManager.GetPrefixAsync(Context.Guild)}trivia start to create a new one.").ConfigureAwait(false);
            }
        }

        [Command("Scores")]
        [Remarks("Gets the global scores")]
        [Example("!trivia scores 10")]
        public async Task GetScoresAsync([Summary("The amount of scores to get.")] int amount = 5)
        {
            string globalScores = await triviaService.GetGlobalHighScoresAsync(amount, Context as SocketCommandContext).ConfigureAwait(false);
            if (globalScores != null)
            {
                var embedBuilder = new EmbedBuilder()
                    .WithColor(new Color(46, 191, 84))
                    .WithTitle("Global scores")
                    .WithDescription(globalScores);
                _ = await ReplyAsync("", embed: embedBuilder.Build()).ConfigureAwait(false);
            }
            else
            {
                _ = await ReplyAsync("No stored scores found!").ConfigureAwait(false);
            }
        }
    }
}