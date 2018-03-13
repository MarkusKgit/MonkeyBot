using Discord;
using Discord.Commands;
using dokas.FluentStrings;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using System;
using System.Collections.Generic;
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
        public async Task AddRoleAsync([Summary("The name of the role to add.")] [Remainder] string roleName = null)
        {
            if (roleName.IsEmpty())
            {
                await ReplyAsync("You need to specify a role you wish to add!" + Environment.NewLine + "Consider using \"!roles list\" to get a list of assignable roles");
                return;
            }
            // Get the role with the specified name
            var role = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == roleName.ToLower());
            if (role == null)
            {
                await ReplyAsync("The role you specified is invalid!" + Environment.NewLine + "Consider using \"!roles list\" to get a list of assignable roles");
                return;
            }
            // Get the role of the bot with permission manage roles
            var botRole = await Helpers.GetManageRolesRoleAsync(Context);
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
            await ReplyAsync($"Role {role.Name} has been added");
        }

        [Command("Remove")]
        [Remarks("Removes the specified role from your roles.")]
        public async Task RemoveRoleAsync([Summary("The role to remove.")] [Remainder] string roleName = null)
        {
            if (roleName.IsEmpty())
            {
                await ReplyAsync("You need to specify a role you wish to remove!");
            }
            // Get the role with the specified name
            var role = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == roleName.ToLower());
            if (role == null)
            {
                await ReplyAsync("The role you specified is invalid!");
            }
            var guser = (IGuildUser)Context.User;
            if (!guser.RoleIds.Contains(role.Id))
            {
                await ReplyAsync("You don't have that role");
            }
            var botRole = await Helpers.GetManageRolesRoleAsync(Context);
            // The bot's role must be higher than the role to be able to remove it
            if (botRole?.Position <= role.Position)
            {
                await ReplyAsync("Insufficient permissions!");
            }
            await guser.RemoveRoleAsync(role);
            await ReplyAsync($"Role {role.Name} has been removed");
        }

        [Command("List")]
        [Remarks("Lists all roles that can be mentioned and assigned.")]
        public async Task ListRolesAsync()
        {
            List<string> allRoles = new List<string>();
            // Get the role of the bot with permission manage roles
            IRole botRole = await Helpers.GetManageRolesRoleAsync(Context);
            // Get all roles that are lower than the bot's role (roles the bot can assign)
            foreach (var role in Context.Guild.Roles)
            {
                if (role.IsMentionable && role.Name != "everyone" && botRole?.Position > role.Position)
                    allRoles.Add(role.Name);
            }
            string msg;
            if (allRoles.Count > 0)
                msg = "The following mentionable roles exist:" + Environment.NewLine + string.Join(", ", allRoles);
            else
                msg = "Now assignable roles exist!";
            await ReplyAsync(msg);
        }

        [Command("ListMembers")]
        [Remarks("Lists all roles and the users who have these roles")]
        public async Task ListMembersAsync()
        {
            var builder = new EmbedBuilder
            {
                Color = new Color(114, 137, 218),
                Description = "These are the are all the assignable roles and the users assigned to them:"
            };
            // Get the role of the bot with permission manage roles
            IRole botRole = await Helpers.GetManageRolesRoleAsync(Context);
            // Get all roles that are lower than the bot's role (roles the bot can assign)
            var guildUsers = await Context.Guild.GetUsersAsync();
            foreach (var role in Context.Guild.Roles)
            {
                if (role.IsMentionable && role.Name != "everyone" && botRole?.Position > role.Position)
                {
                    var roleUsers = guildUsers?.Where(x => x.RoleIds.Contains(role.Id)).Select(x => x.Username).OrderBy(x => x);
                    if (roleUsers != null && roleUsers.Count() > 0)
                    {
                        builder.AddField(x =>
                        {
                            x.Name = role.Name;
                            x.Value = string.Join(", ", roleUsers);
                            x.IsInline = false;
                        });
                    }
                }
            }
            await Context.User.SendMessageAsync("", false, builder.Build());
        }

        [Command("ListMembers")]
        [Remarks("Lists all the members of the specified role")]
        public async Task ListMembersAsync(string roleName)
        {
            var role = Context.Guild.Roles.SingleOrDefault(x => x.Name.ToLower() == roleName.ToLower());
            if (role == null)
            {
                await ReplyAsync($"Role not found! Use roles list to get a list of all roles.");
                return;
            }
            var guildUsers = await Context.Guild.GetUsersAsync();
            var roleUsers = guildUsers?.Where(x => x.RoleIds.Contains(role.Id)).Select(x => x.Username).OrderBy(x => x);
            if (roleUsers == null || roleUsers.Count() < 1)
                return;
            var builder = new EmbedBuilder
            {
                Color = new Color(114, 137, 218),
                Description = $"These are the users assigned to the {role.Name} role:"
            };
            builder.AddField(x =>
                {
                    x.Name = role.Name;
                    x.Value = string.Join(", ", roleUsers);
                    x.IsInline = false;
                });
            await Context.User.SendMessageAsync("", false, builder.Build());
        }
    }
}