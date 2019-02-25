using Discord;
using Discord.Commands;
using dokas.FluentStrings;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    /// <summary>A module that provides help commands</summary>
    [Name("Help")]
    [MinPermissions(AccessLevel.User)]
    public class HelpModule : MonkeyModuleBase
    {
        private readonly CommandManager commandManager;
        private readonly CommandService commandService;

        public HelpModule(CommandManager commandManager, CommandService commandService)
        {
            this.commandManager = commandManager;
            this.commandService = commandService;
        }

        [Command("help")]
        [Remarks("List all usable commands.")]
        public async Task HelpAsync()
        {
            string prefix = await commandManager.GetPrefixAsync(Context.Guild);
            var embedBuilder = new EmbedBuilder
            {
                Color = new Color(114, 137, 218),
                Description = "These are the commands you can use with your permission level"
            };

            foreach (var module in commandService.Modules)
            {
                var builder = new StringBuilder();
                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (result.IsSuccess)
                    {
                        string parameters = string.Empty;
                        if (cmd.Parameters != null && cmd.Parameters.Count > 0)
                            parameters = $"*{string.Join(" ", cmd.Parameters.Select(x => x.Name))}*";
                        builder.AppendLine($"{prefix}{cmd.Aliases.First()}  {parameters}");
                    }
                }
                var description = builder.ToString();

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
            if (Context.Channel is IGuildChannel)
                await ReplyAndDeleteAsync("I have sent you a private message");
        }

        [Command("help")]
        [Remarks("Gets help for the specified command")]
        [Example("!help Chuck")]
        public async Task HelpAsync([Summary("The command to get help for.")] [Remainder]string command)
        {
            var result = commandService.Search(Context, command);

            if (!result.IsSuccess)
            {
                await Context.User.SendMessageAsync($"Sorry, I couldn't find a command like **{command}**.");
                return;
            }

            string prefix = await commandManager.GetPrefixAsync(Context.Guild);
            var builder = new EmbedBuilder
            {
                Color = new Color(114, 137, 218),
                Description = $"These are commands like **{command}**:"
            };

            foreach (var match in result.Commands)
            {
                var cmd = match.Command;
                const string separator = ", ";
                var paramBuilder = new StringBuilder();
                foreach (var param in cmd.Parameters)
                {
                    paramBuilder.Append(param.Name);
                    if (!param.Summary.IsEmpty().OrWhiteSpace())
                        paramBuilder.Append($" **({param.Summary})**");
                    paramBuilder.Append(separator);
                }

                var cmdParameters = paramBuilder.ToString().TrimEnd(separator.ToArray());
                string description =
                    $"Parameters: {cmdParameters}\n" +
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
            if (Context.Channel is IGuildChannel)
                await ReplyAndDeleteAsync("I have sent you a private message");
        }
    }
}