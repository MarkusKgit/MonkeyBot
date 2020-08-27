using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using MonkeyBot.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Common
{
    /// <summary>
    /// Set the minimum permission required to use a module or command
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class MinPermissionsAttribute : CheckBaseAttribute
    {
        public MinPermissionsAttribute(AccessLevel level)
        {
            AccessLevel = level;
        }

        public AccessLevel AccessLevel { get; }

        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            AccessLevel access = await GetPermissionAsync(ctx).ConfigureAwait(false); // Get the acccesslevel for this context

            return access >= AccessLevel;
        }

        public async static Task<AccessLevel> GetPermissionAsync(CommandContext ctx)
        {
            if (ctx.User.IsBot) // Prevent other bots from executing commands.
            {
                return AccessLevel.Blocked;
            }

            DiscordClientConfiguration config = await DiscordClientConfiguration.LoadAsync().ConfigureAwait(false);
            IReadOnlyList<ulong> owners = config.Owners;
            if (owners != null && owners.Contains(ctx.User.Id)) // Give configured owners special access.
            {
                return AccessLevel.BotOwner;
            }

            // Check if the context is in a guild.
            if (ctx.Guild != null)
            {
                if (ctx.Guild.Owner.Id == ctx.User.Id) // Check if the user is the guild owner.
                {
                    return AccessLevel.ServerOwner;
                }

                if (ctx.Guild.Permissions.HasValue
                    && ctx.Guild.Permissions.Value.HasPermission(Permissions.Administrator))
                {
                    return AccessLevel.ServerAdmin;
                }

                if (ctx.Guild.Permissions.HasValue
                    && ctx.Guild.Permissions.Value.HasPermission(Permissions.ManageMessages)
                    && ctx.Guild.Permissions.Value.HasPermission(Permissions.BanMembers)
                    && ctx.Guild.Permissions.Value.HasPermission(Permissions.KickMembers))
                {
                    return AccessLevel.ServerMod;
                }
            }

            return AccessLevel.User; // If nothing else, return a default permission.
        }        
    }
}