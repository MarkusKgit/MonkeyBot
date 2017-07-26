﻿using Discord;
using Discord.Commands;
using MonkeyBot.Common;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    /// <summary>A module that provides help commands</summary>
    [Name("Help")]
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        private CommandService commandService;

        public HelpModule(CommandService service) // Create a constructor for the commandservice dependency
        {
            commandService = service;
        }

        [Command("help")]
        [Remarks("List all usable commands.")]
        public async Task HelpAsync()
        {
            string prefix = (await Configuration.LoadAsync()).Prefix;
            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = "These are the commands you can use with your permission level"
            };

            foreach (var module in commandService.Modules)
            {
                string description = null;
                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (result.IsSuccess)
                    {
                        string parameters = string.Empty;
                        if (cmd.Parameters != null && cmd.Parameters.Count > 0)
                            parameters = "*" + cmd.Parameters.Select(x => x.Name).Aggregate((a, b) => (a + " " + b)) + "*";
                        description += $"{prefix}{cmd.Aliases.First()}  {parameters}{Environment.NewLine}";
                    }
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    builder.AddField(x =>
                    {
                        x.Name = module.Name;
                        x.Value = description;
                        x.IsInline = false;
                    });
                }
            }
            await Context.User.SendMessageAsync("", false, builder.Build());
        }

        [Command("help")]
        [Remarks("Gets help for the specified command")]
        public async Task HelpAsync([Summary("The command to get help for.")] [Remainder]string command)
        {
            var result = commandService.Search(Context, command);

            if (!result.IsSuccess)
            {
                await Context.User.SendMessageAsync($"Sorry, I couldn't find a command like **{command}**.");
                return;
            }

            string prefix = (await Configuration.LoadAsync()).Prefix;
            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = $"These are the commands like **{command}**:"
            };

            foreach (var match in result.Commands)
            {
                var cmd = match.Command;

                builder.AddField(x =>
                {
                    x.Name = string.Join(", ", cmd.Aliases);
                    x.Value = $"Parameters: {string.Join(", ", cmd.Parameters.Select(p => p.Name))}\n" +
                              $"Remarks: {cmd.Remarks}";
                    x.IsInline = false;
                });
            }
            await Context.User.SendMessageAsync("", false, builder.Build());
        }
    }
}