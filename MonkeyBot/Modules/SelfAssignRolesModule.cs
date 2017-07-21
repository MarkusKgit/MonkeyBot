using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [Group("Roles")]
    public class SelfAssignRolesModule : ModuleBase
    {
        // ~Roles Add -role-
        [Command("Add"), Summary("Adds the specified role.")]
        public async Task Add([Summary("The role to add.")] [Remainder] string roleName = null)
        {
            var role = Context.Guild.Roles.Where(x => x.Name.ToLower() == roleName.ToLower()).FirstOrDefault();
            var guser = (IGuildUser)Context.User;
            var botRole = await GetBotRole();
            if (string.IsNullOrEmpty(roleName))
            {
                await ReplyAsync("You need to specify a role you wish to add!" + Environment.NewLine + "Consider using \"!roles list\" to get a list of assignable roles");
            }
            else if (role == null)
            {
                await ReplyAsync("The role you specified is invalid!" + Environment.NewLine + "Consider using \"!roles list\" to get a list of assignable roles");
            }
            else if (botRole?.Position <= role.Position)
            {
                await ReplyAsync("Insufficient permissions!");
            }
            else if (guser.RoleIds.Contains(role.Id))
            {
                await ReplyAsync("You already have that role");
            }
            else
            {
                await guser.AddRoleAsync(role);
                await ReplyAsync(string.Format("Role {0} has been added", role.Name));
            }
        }

        // ~Roles Remove -role-
        [Command("Remove"), Summary("Removes the specified role.")]
        public async Task Remove([Summary("The role to remove.")] [Remainder] string roleName = null)
        {
            var role = Context.Guild.Roles.Where(x => x.Name.ToLower() == roleName.ToLower()).FirstOrDefault();
            var guser = (IGuildUser)Context.User;
            var botRole = await GetBotRole();
            if (string.IsNullOrEmpty(roleName))
            {
                await ReplyAsync("You need to specify a role you wish to remove!");
            }
            else if (role == null)
            {
                await ReplyAsync("The role you specified is invalid!");
            }
            else if (!guser.RoleIds.Contains(role.Id))
            {
                await ReplyAsync("You don't have that role");
            }
            else if (botRole?.Position <= role.Position)
            {
                await ReplyAsync("Insufficient permissions!");
            }
            else
            {
                await guser.RemoveRoleAsync(role);
                await ReplyAsync(string.Format("Role {0} has been removed", role.Name));
            }
        }

        // ~Roles List
        [Command("list"), Summary("Lists all assignable roles that can be mentioned")]
        public async Task List()
        {
            string allRoles = string.Empty;
            IRole ownrole = await GetBotRole();
            foreach (var role in Context.Guild.Roles)
            {
                if (role.IsMentionable && role.Name != "everyone" && ownrole?.Position > role.Position)
                    allRoles += role.Name + ",";
            }
            allRoles = allRoles.Remove(allRoles.Count() - 1);
            string msg;
            if (allRoles != string.Empty)
                msg = "The following mentionable roles exist:" + Environment.NewLine + allRoles;
            else
                msg = "Now assignable roles exist!";
            await ReplyAsync(msg);
        }

        private async Task<IRole> GetBotRole()
        {
            var thisBot = await Context.Guild.GetUserAsync(Context.Client.CurrentUser.Id);
            var ownrole = Context.Guild.Roles.Where(x => x.Permissions.ManageRoles == true && x.Id == thisBot.RoleIds.Max()).FirstOrDefault();
            return ownrole;
        }
    }
}