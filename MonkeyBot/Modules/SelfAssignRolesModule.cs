using Discord;
using Discord.Commands;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    /// <summary>Module that handles role assignments</summary>
    [Group("Roles")]
    [Name("Roles")]
    [MinPermissions(AccessLevel.User)]
    [RequireContext(ContextType.Guild)]
    public class SelfAssignRolesModule : ModuleBase
    {
        [Command("Add")]
        [Remarks("Adds the specified role to your own roles.")]
        public async Task AddAsync([Summary("The name of the role to add.")] [Remainder] string roleName = null)
        {
            if (string.IsNullOrEmpty(roleName))
            {
                await ReplyAsync("You need to specify a role you wish to add!" + Environment.NewLine + "Consider using \"!roles list\" to get a list of assignable roles");
                return;
            }
            // Get the role with the specified name
            var role = Context.Guild.Roles.Where(x => x.Name.ToLower() == roleName.ToLower()).FirstOrDefault();
            if (role == null)
            {
                await ReplyAsync("The role you specified is invalid!" + Environment.NewLine + "Consider using \"!roles list\" to get a list of assignable roles");
                return;
            }
            // Get the role of the bot with permission manage roles
            var botRole = await Helpers.GetBotRoleAsync(Context);
            // The bot's role must be higher than the role to be able to assign it
            if (botRole?.Position <= role.Position)
            {
                await ReplyAsync("Insufficient permissions!");
                return;
            }
            var guser = (IGuildUser)Context.User;
            if (guser.RoleIds.Contains(role.Id))
            {
                await ReplyAsync("You already have that role");
                return;
            }
            await guser.AddRoleAsync(role);
            await ReplyAsync(string.Format("Role {0} has been added", role.Name));
        }

        [Command("Remove")]
        [Remarks("Removes the specified role from your roles.")]
        public async Task RemoveAsync([Summary("The role to remove.")] [Remainder] string roleName = null)
        {
            if (string.IsNullOrEmpty(roleName))
            {
                await ReplyAsync("You need to specify a role you wish to remove!");
            }
            // Get the role with the specified name
            var role = Context.Guild.Roles.Where(x => x.Name.ToLower() == roleName.ToLower()).FirstOrDefault();
            if (role == null)
            {
                await ReplyAsync("The role you specified is invalid!");
            }
            var guser = (IGuildUser)Context.User;
            if (!guser.RoleIds.Contains(role.Id))
            {
                await ReplyAsync("You don't have that role");
            }
            var botRole = await Helpers.GetBotRoleAsync(Context);
            // The bot's role must be higher than the role to be able to remove it
            if (botRole?.Position <= role.Position)
            {
                await ReplyAsync("Insufficient permissions!");
            }
            await guser.RemoveRoleAsync(role);
            await ReplyAsync(string.Format("Role {0} has been removed", role.Name));
        }

        [Command("List")]
        [Remarks("Lists all roles that can be mentioned and assigned.")]
        public async Task ListAsync()
        {
            string allRoles = string.Empty;
            // Get the role of the bot with permission manage roles
            IRole botRole = await Helpers.GetBotRoleAsync(Context);
            // Get all roles that are lower than the bot's role (roles the bot can assign)
            foreach (var role in Context.Guild.Roles)
            {
                if (role.IsMentionable && role.Name != "everyone" && botRole?.Position > role.Position)
                    allRoles += role.Name + ",";
            }
            allRoles = allRoles.Remove(allRoles.Count() - 1); // remove the last comma
            string msg;
            if (allRoles != string.Empty)
                msg = "The following mentionable roles exist:" + Environment.NewLine + allRoles;
            else
                msg = "Now assignable roles exist!";
            await ReplyAsync(msg);
        }

        
    }
}