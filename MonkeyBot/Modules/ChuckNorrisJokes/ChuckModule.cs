﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MonkeyBot.Common;
using MonkeyBot.Services;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [MinPermissions(AccessLevel.User)]
    [RequireGuild]
    [Description("Chuck Norris jokes")]
    public class ChuckModule : BaseCommandModule
    {
        private readonly IChuckService _chuckService;

        public ChuckModule(IChuckService chuckService)
        {
            _chuckService = chuckService;
        }

        [Command("Chuck")]
        [Priority(0)]
        [Description("Gets a random Chuck Norris fact.")]
        public async Task GetChuckFactAsync(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();
            string fact = await (_chuckService?.GetChuckFactAsync());
            _ = fact.IsEmpty()
                ? await ctx.ErrorAsync("Could not get a chuck fact :(")
                : await ctx.OkAsync(fact, "Random Chuck Norris fact", false);
        }

        [Command("Chuck")]
        [Priority(1)]
        [Description("Gets a random Chuck Norris fact and replaces Chuck Norris with the given name.")]
        public async Task GetChuckFactAsync(CommandContext ctx, [RemainingText][Description("The person to chuck")] DiscordMember user)
        {
            await ctx.TriggerTypingAsync();
            if (user == null)
            {
                await ctx.ErrorAsync("Invalid User");
            }            
            string fact = await (_chuckService?.GetChuckFactAsync(user.DisplayName));
            if (fact.IsEmpty())
            {
                await ctx.ErrorAsync("Could not get a chuck fact :(");
                return;
            }            
            await ctx.OkAsync(fact, $"Random Chuck \"*{user.DisplayName}*\" Norris fact", false);
        }
    }
}