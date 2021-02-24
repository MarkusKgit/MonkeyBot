using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MonkeyBot.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    public class GuildInfoModule : BaseCommandModule
    {
        //private readonly MonkeyDBContext dbContext;
        private readonly IGuildService guildService;

        public GuildInfoModule(IGuildService guildService)
        {
            this.guildService = guildService;
        }

        [Command("Rules")]
        [Description("The bot replies with the server rules")]
        [RequireGuild]
        public async Task ListRulesAsync(CommandContext ctx)
        {
            List<string> rules = (await guildService.GetOrCreateConfigAsync(ctx.Guild.Id)).Rules;
            if (rules == null || rules.Count < 1)
            {
                _ = await ctx.RespondAsync("No rules set!");
                return;
            }
            var builder = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.DarkGreen)
                .WithTitle($"Rules of {ctx.Guild.Name}")
                .WithDescription(string.Join(Environment.NewLine, rules));

            _ = await ctx.RespondDeletableAsync(embed: builder.Build());

        }
    }
}