﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MonkeyBot.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [Description("Guild Info")]
    public class GuildInfoModule : BaseCommandModule
    {
        private readonly IGuildService _guildService;
        
        public GuildInfoModule(IGuildService guildService)
        {
            _guildService = guildService;
        }

        [Command("Rules")]
        [Description("The bot replies with the server rules")]
        [RequireGuild]
        public async Task ListRulesAsync(CommandContext ctx)
        {
            List<string> rules = (await _guildService.GetOrCreateConfigAsync(ctx.Guild.Id)).Rules;
            if (rules == null || rules.Count < 1)
            {
                await ctx.RespondAsync("No rules set!");
                return;
            }
            var builder = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.DarkGreen)
                .WithTitle($"Rules of {ctx.Guild.Name}")
                .WithDescription(string.Join(Environment.NewLine, rules));

            await ctx.RespondDeletableAsync(builder.Build());

        }
    }
}