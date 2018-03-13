using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Preconditions
{
    /// <summary>
    /// An attribute that the defines the minimum permission level to be admin
    /// </summary>
    public sealed class RequireAdminAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var caller = context.User as IGuildUser;
            if (caller == null)
                return Task.FromResult(PreconditionResult.FromError("User not valid"));
            if (caller.GuildPermissions.Administrator)
                return Task.FromResult(PreconditionResult.FromSuccess());
            else
                return Task.FromResult(PreconditionResult.FromError("You must be administrator to run this command"));
        }
    }
}