using Discord;
using Discord.Commands;
using dokas.FluentStrings;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    /// <summary>A module that provides help commands</summary>
    [Name("Help")]
    [MinPermissions(AccessLevel.User)]
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        private readonly CommandManager commandManager;

        public HelpModule(CommandManager commandManager)
        {
            this.commandManager = commandManager;
        }

        [Command("help")]
        [Remarks("List all usable commands.")]
        public async Task HelpAsync()
        {
            string prefix = await commandManager.GetPrefixAsync(Context.Guild);
            var embedBuilder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = "These are the commands you can use with your permission level"
            };

            foreach (var module in commandManager.CommandService.Modules)
            {
                string description = null;
                var builder = new System.Text.StringBuilder();
                builder.Append(description);
                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (result.IsSuccess)
                    {
                        string parameters = string.Empty;
                        if (cmd.Parameters != null && cmd.Parameters.Count > 0)
                            parameters = "*" + cmd.Parameters.Select(x => x.Name).Aggregate((a, b) => (a + " " + b)) + "*";
                        builder.Append($"{prefix}{cmd.Aliases.First()}  {parameters}{Environment.NewLine}");
                    }
                }
                description = builder.ToString();

                if (!description.IsEmpty().OrWhiteSpace())
                {
                    embedBuilder.AddField(x =>
                    {
                        x.Name = module.Name;
                        x.Value = description;
                        x.IsInline = false;
                    });
                }
            }
            await Context.User.SendMessageAsync("", false, embedBuilder.Build());
        }

        [Command("help")]
        [Remarks("Gets help for the specified command")]
        [Example("!help Chuck")]
        public async Task HelpAsync([Summary("The command to get help for.")] [Remainder]string command)
        {
            var result = commandManager.CommandService.Search(Context, command);

            if (!result.IsSuccess)
            {
                await Context.User.SendMessageAsync($"Sorry, I couldn't find a command like **{command}**.");
                return;
            }

            string prefix = await commandManager.GetPrefixAsync(Context.Guild);
            var builder = new EmbedBuilder
            {
                Color = new Color(114, 137, 218),
                Description = $"These are the commands like **{command}**:"
            };

            foreach (var match in result.Commands)
            {
                var cmd = match.Command;
                string description =
                    $"Parameters: {string.Join(", ", cmd.Parameters.Select(p => p.Name))}\n" +
                    $"Remarks: {cmd.Remarks}";
                var example = cmd.Attributes.OfType<ExampleAttribute>().FirstOrDefault();
                if (example != null && !example.ExampleText.IsEmpty())
                    description += $"\nExample: {example.ExampleText}";
                builder.AddField(x =>
                {
                    x.Name = string.Join(", ", cmd.Aliases);
                    x.Value = description;
                    x.IsInline = false;
                });
            }
            await Context.User.SendMessageAsync("", false, builder.Build());
        }
    }
}