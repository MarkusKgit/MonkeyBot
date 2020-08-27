using Discord;
using Discord.Commands;
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
    [Description("Roles")]
    [MinPermissions(AccessLevel.User)]
    [RequireGuild]
    [RequireBotPermission(GuildPermission.ManageRoles)]
    public class SelfAssignRolesModule : MonkeyModuleBase
    {
        [Command("Add")]
        [Description("Adds the specified role to your own roles.")]
        [Example("!roles add bf")]
        public async Task AddRoleAsync([Description("The name of the role to add.")] [RemainingText] string roleName)
        {
            // Get the role with the specified name
            IRole role = await GetRoleInGuildAsync(roleName).ConfigureAwait(false);
            if (role == null)
            {
                return;
            }
            // Get the role of the bot with permission manage roles
            IRole botRole = await GetManageRolesRoleAsync().ConfigureAwait(false);
            // The bot's role must be higher than the role to be able to assign it
            if (botRole?.Position <= role.Position)
            {
                _ = await ctx.RespondAsync("Insufficient permissions!").ConfigureAwait(false);
                return;
            }
            var guser = (IGuildUser)Context.User;
            if (guser.RoleIds.Contains(role.Id))
            {
                _ = await ctx.RespondAsync("You already have that role").ConfigureAwait(false);
                return;
            }
            await guser.AddRoleAsync(role).ConfigureAwait(false);
            await ReplyAndDeleteAsync($"Role {role.Name} has been added").ConfigureAwait(false);
        }

        [Command("Remove")]
        [Description("Removes the specified role from your roles.")]
        [Example("!roles remove bf")]
        public async Task RemoveRoleAsync([Description("The role to remove.")] [RemainingText] string roleName = null)
        {
            IRole role = await GetRoleInGuildAsync(roleName).ConfigureAwait(false);
            if (role == null)
            {
                return;
            }
            var guser = (IGuildUser)Context.User;
            if (!guser.RoleIds.Contains(role.Id))
            {
                _ = await ctx.RespondAsync("You don't have that role").ConfigureAwait(false);
            }
            IRole botRole = await GetManageRolesRoleAsync().ConfigureAwait(false);
            // The bot's role must be higher than the role to be able to remove it
            if (botRole?.Position <= role.Position)
            {
                _ = await ctx.RespondAsync("Insufficient permissions!").ConfigureAwait(false);
            }
            await guser.RemoveRoleAsync(role).ConfigureAwait(false);
            await ReplyAndDeleteAsync($"Role {role.Name} has been removed").ConfigureAwait(false);
        }

        [Command("List")]
        [Description("Lists all roles that can be mentioned and assigned.")]
        public async Task ListRolesAsync()
        {
            var allRoles = new List<string>();
            // Get the role of the bot with permission manage roles
            IRole botRole = await GetManageRolesRoleAsync().ConfigureAwait(false);
            // Get all roles that are lower than the bot's role (roles the bot can assign)
            foreach (IRole role in Context.Guild.Roles)
            {
                if (role.IsMentionable && role.Name != "everyone" && botRole?.Position > role.Position)
                {
                    allRoles.Add(role.Name);
                }
            }
            string msg = allRoles.Count > 0
                ? "The following assignable roles exist:" + Environment.NewLine + string.Join(", ", allRoles)
                : "No assignable roles exist!";
            _ = await ctx.RespondAsync(msg).ConfigureAwait(false);
        }

        [Command("ListMembers")]
        [Description("Lists all roles and the users who have these roles")]
        public async Task ListMembersAsync()
        {
            var builder = new EmbedBuilder
            {
                Color = new Color(114, 137, 218),
                Description = "These are the are all the assignable roles and the users assigned to them:"
            };
            // Get the role of the bot with permission manage roles
            IRole botRole = await GetManageRolesRoleAsync().ConfigureAwait(false);
            // Get all roles that are lower than the bot's role (roles the bot can assign)
            IReadOnlyCollection<IGuildUser> guildUsers = await Context.Guild.GetUsersAsync().ConfigureAwait(false);
            foreach (IRole role in Context.Guild.Roles)
            {
                if (role.IsMentionable && role.Name != "everyone" && botRole?.Position > role.Position)
                {
                    IOrderedEnumerable<string> roleUsers = guildUsers?.Where(x => x.RoleIds.Contains(role.Id))
                                                                      .Select(x => x.Username)
                                                                      .OrderBy(x => x);
                    if (roleUsers != null && roleUsers.Any())
                    {
                        _ = builder.AddField(x =>
                          {
                              x.Name = role.Name;
                              x.Value = string.Join(", ", roleUsers);
                              x.IsInline = false;
                          });
                    }
                }
            }
            _ = await Context.User.SendMessageAsync("", false, builder.Build()).ConfigureAwait(false);
            await ReplyAndDeleteAsync("I have sent you a private message").ConfigureAwait(false);
        }

        [Command("ListMembers")]
        [Description("Lists all the members of the specified role")]
        [Example("!roles listmembers bf")]
        public async Task ListMembersAsync(string roleName)
        {
            IRole role = await GetRoleInGuildAsync(roleName).ConfigureAwait(false);
            if (role == null)
            {
                return;
            }
            IReadOnlyCollection<IGuildUser> guildUsers = await Context.Guild.GetUsersAsync().ConfigureAwait(false);
            IOrderedEnumerable<string> roleUsers = guildUsers?.Where(x => x.RoleIds.Contains(role.Id))
                                                              .Select(x => x.Username)
                                                              .OrderBy(x => x);
            if (roleUsers == null || !roleUsers.Any())
            {
                return;
            }
            var builder = new EmbedBuilder
            {
                Color = new Color(114, 137, 218),
                Description = $"These are the users assigned to the {role.Name} role:"
            };
            _ = builder.AddField(x =>
                  {
                      x.Name = role.Name;
                      x.Value = string.Join(", ", roleUsers);
                      x.IsInline = false;
                  });
            _ = await Context.User.SendMessageAsync("", false, builder.Build()).ConfigureAwait(false);
            await ReplyAndDeleteAsync("I have sent you a private message").ConfigureAwait(false);
        }
    }
}