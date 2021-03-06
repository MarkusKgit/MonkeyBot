﻿using Discord;
using Discord.Commands;
using MonkeyBot.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Preconditions
{
    /// <summary>
    /// Set the minimum permission required to use a module or command
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class MinPermissionsAttribute : PreconditionAttribute
    {
        public MinPermissionsAttribute(AccessLevel level)
        {
            AccessLevel = level;
        }

        public AccessLevel AccessLevel { get; }

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            AccessLevel access = await GetPermissionAsync(context).ConfigureAwait(false); // Get the acccesslevel for this context

            return access >= AccessLevel ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("Insufficient permissions");
        }

        public async static Task<AccessLevel> GetPermissionAsync(ICommandContext c)
        {
            if (c.User.IsBot) // Prevent other bots from executing commands.
            {
                return AccessLevel.Blocked;
            }

            DiscordClientConfiguration config = await DiscordClientConfiguration.LoadAsync().ConfigureAwait(false);
            IReadOnlyList<ulong> owners = config.Owners;
            if (owners != null && owners.Contains(c.User.Id)) // Give configured owners special access.
            {
                return AccessLevel.BotOwner;
            }

            // Check if the context is in a guild.
            if (c.User is IGuildUser user)
            {
                if (c.Guild.OwnerId == user.Id) // Check if the user is the guild owner.
                {
                    return AccessLevel.ServerOwner;
                }

                if (user.GuildPermissions.Administrator) // Check if the user has the administrator permission.
                {
                    return AccessLevel.ServerAdmin;
                }

                if (user.GuildPermissions.ManageMessages || // Check if the user can ban, kick, or manage messages.
                    user.GuildPermissions.BanMembers ||
                    user.GuildPermissions.KickMembers)
                {
                    return AccessLevel.ServerMod;
                }
            }

            return AccessLevel.User; // If nothing else, return a default permission.
        }
    }
}