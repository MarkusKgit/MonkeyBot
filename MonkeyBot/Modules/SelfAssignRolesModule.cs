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
        [Command("Add"), Summary("Addes the specified role.")]
        public async Task Add([Summary("The role to add.")] [Remainder] IRole role = null)
        {
            var guser = (IGuildUser)Context.User;
            if (role != null)
            {
                await guser.AddRoleAsync(role);
                await ReplyAsync(string.Format("Role {0} has been added", role.Name));
            }
        }

        [Command("Remove"), Summary("Removes the specified role.")]
        public async Task Remove([Summary("The role to remove.")] [Remainder] IRole role = null)
        {
            var guser = (IGuildUser)Context.User;
            if (role != null && guser.RoleIds.Contains(role.Id))
            {
                await guser.RemoveRoleAsync(role);
                await ReplyAsync(string.Format("Role {0} has been removed", role.Name));
            }

        }

        [Command("list"), Summary("Lists all assignable roles")]
        public async Task List()
        {
            string allRoles = string.Empty;            
            var thisBot = await Context.Guild.GetUserAsync(Context.Client.CurrentUser.Id);
            var ownrole = Context.Guild.Roles.Where(x => x.Id == thisBot.RoleIds.Max()).FirstOrDefault();

            foreach (var role in Context.Guild.Roles)
            {
                if (role.IsMentionable && role.Name != "everyone" && ownrole.Position > role.Position )
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

        [Command("Ping"), Summary("Pings everyone with the specified role.")]
        public async Task Ping([Summary("The name of the role to ping.")] [Remainder] IRole role = null)
        {
            var guser = (IGuildUser)Context.User;

            if (role == null)
                role = guser.Guild.Roles.FirstOrDefault();
            if (role != null)
                await ReplyAsync(role.Name);

        }
    }
}
