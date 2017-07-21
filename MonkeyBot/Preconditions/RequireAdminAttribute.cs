using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Preconditions
{
    public class RequireAdminAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var caller = context.User as IGuildUser;
            if (caller == null)
                return new Task<PreconditionResult>(() => PreconditionResult.FromError("User not valid"));
            if (caller.GuildPermissions.Administrator)
                return new Task<PreconditionResult>(() => PreconditionResult.FromSuccess());
            else
                return new Task<PreconditionResult>(() => PreconditionResult.FromError("You must be administrator to run this command"));
        }
    }
}