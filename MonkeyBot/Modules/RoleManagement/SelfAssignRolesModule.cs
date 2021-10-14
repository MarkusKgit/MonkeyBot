using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using MonkeyBot.Common;
using MonkeyBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    //TODO: Consolidate with role buttons and make assignable roles configurable

    /// <summary>Module that handles role assignments</summary>    
    [Description("Self role management")]
    [MinPermissions(AccessLevel.User)]
    [RequireGuild]
    public class SelfAssignRolesModule : BaseCommandModule
    {
        private InteractivityExtension _interactivityExtension;
        private readonly IRoleManagementService _roleManagementService;

        public SelfAssignRolesModule(IRoleManagementService roleManagementService)
        {
            _roleManagementService = roleManagementService;
        }

        [Command("GiveRole")]
        [Aliases(new[] { "GrantRole", "AddRole" })]
        [Description("Adds the specified role to your own roles.")]
        [Example("giverole")]
        [Example("giverole @bf")]
        public async Task AddRoleAsync(CommandContext ctx, [RemainingText, Description("The role you want to have")] DiscordRole role = null)
        {
            DiscordRole roleToAssign = role;
            DiscordMember member = ctx.Member;
            DiscordRole botRole = await GetBotRoleAsync(ctx.Client.CurrentUser, ctx.Guild);
            if (roleToAssign == null)
            {
                _interactivityExtension = ctx.Client.GetInteractivity();
                var assignableRoles = GetAssignableRoles(botRole, ctx.Guild).Except(ctx.Member.Roles);
                if(!assignableRoles.Any())
                {
                    await ctx.ErrorAsync("You already have all the roles I can assign");
                    return;
                }
                (member, roleToAssign) = await GetUserRoleSelectionAsync(assignableRoles, ctx.Guild, ctx.Channel, "assignableRoles", "Roles to assign");
            }

            if (roleToAssign == null)
            {
                await ctx.ErrorAsync("Invalid role");
                return;
            }

            // The bot's role must be higher than the role to be able to assign it
            if (botRole == null || botRole?.Position <= roleToAssign.Position)
            {
                await ctx.ErrorAsync("Sorry, I don't have sufficient permissions to give you this role!");
                return;
            }

            if (member.Roles.Contains(roleToAssign))
            {
                await ctx.ErrorAsync("You already have that role");
                return;
            }
            await member.GrantRoleAsync(roleToAssign);
            await ctx.OkAsync($"Role {roleToAssign.Name} has been added");
        }

        [Command("RemoveRole")]
        [Aliases(new[] { "RevokeRole" })]
        [Description("Removes the specified role from your roles.")]
        [Example("RemoveRole")]
        [Example("RemoveRole @bf")]
        public async Task RemoveRoleAsync(CommandContext ctx, [RemainingText, Description("The role you want to get rid of")] DiscordRole role = null)
        {
            DiscordRole roleToRemove = role;
            DiscordMember member = ctx.Member;
            DiscordRole botRole = await GetBotRoleAsync(ctx.Client.CurrentUser, ctx.Guild);
            if (roleToRemove == null)
            {
                _interactivityExtension = ctx.Client.GetInteractivity();
                var rolesToRevoke = ctx.Member.Roles;
                if(!rolesToRevoke.Any())
                {
                    await ctx.ErrorAsync("You don't have any roles to revoke");
                    return;
                }
                (member, roleToRemove) = await GetUserRoleSelectionAsync(rolesToRevoke, ctx.Guild, ctx.Channel, "revocableRoles", "Roles to revoke");
            }

            if (roleToRemove == null)
            {
                await ctx.ErrorAsync("Invalid role");
                return;
            }

            if (!member.Roles.Contains(roleToRemove))
            {
                await ctx.ErrorAsync("You don't have that role");
                return;
            }

            // The bot's role must be higher than the role to be able to remove it
            if (botRole == null || botRole?.Position <= roleToRemove.Position)
            {
                await ctx.ErrorAsync("Sorry, I don't have sufficient permissions to take this role from you!");
            }
            await member.RevokeRoleAsync(roleToRemove);
            await ctx.OkAsync($"Role {roleToRemove.Name} has been revoked");
        }

        [Command("ListRoles")]
        [Description("Lists all roles that can be mentioned and assigned.")]
        public async Task ListRolesAsync(CommandContext ctx)
        {
            DiscordRole botRole = await GetBotRoleAsync(ctx.Client.CurrentUser, ctx.Guild);
            IEnumerable<DiscordRole> assignableRoles = GetAssignableRoles(botRole, ctx.Guild);
            if (assignableRoles.Any())
            {
                string roles = string.Join(", ", assignableRoles.OrderBy(r => r.Name).Select(r => r.Name));
                await ctx.OkAsync(roles, "The following assignable roles exist");
            }
            else
            {
                await ctx.ErrorAsync("No assignable roles exist!");
            }
        }

        [Command("ListRolesWithMembers")]
        [Description("Lists all roles and the users who have these roles")]
        public async Task ListMembersAsync(CommandContext ctx)
        {
            var builder = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor(114, 137, 218))
                .WithDescription("These are the are all the assignable roles and the users assigned to them:");

            DiscordRole botRole = await GetBotRoleAsync(ctx.Client.CurrentUser, ctx.Guild);
            IEnumerable<DiscordRole> assignableRoles = GetAssignableRoles(botRole, ctx.Guild);
            IReadOnlyCollection<DiscordMember> guildMembers = await ctx.Guild.GetAllMembersAsync();
            foreach (DiscordRole role in assignableRoles)
            {
                IOrderedEnumerable<string> roleUsers = guildMembers?.Where(m => m.Roles.Contains(role))
                                                                    .Select(x => x.Username)
                                                                    .OrderBy(x => x);
                _ = roleUsers != null && roleUsers.Any()
                    ? builder.AddField(role.Name, string.Join(", ", roleUsers), false)
                    : builder.AddField(role.Name, "-", false);
            }
            await ctx.RespondDeletableAsync(builder.Build());
        }

        [Command("ListRoleMembers")]
        [Description("Lists all the members of the specified role")]
        [Example("ListRoleMembers @bf")]
        public async Task ListMembersAsync(CommandContext ctx, [Description("The role to display members for")] DiscordRole role)
        {
            IReadOnlyCollection<DiscordMember> guildMembers = await ctx.Guild.GetAllMembersAsync();
            IOrderedEnumerable<string> roleUsers = guildMembers?.Where(x => x.Roles.Contains(role))
                                                              .Select(x => x.Username)
                                                              .OrderBy(x => x);
            if (roleUsers == null || !roleUsers.Any())
            {
                await ctx.ErrorAsync("This role does not have any members!");
                return;
            }
            var builder = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor(114, 137, 218))
                .WithTitle($"These are the users assigned to the {role.Name} role:")
                .WithDescription(string.Join(", ", roleUsers));

            await ctx.RespondDeletableAsync(builder.Build());
        }

        private async Task<(DiscordMember member, DiscordRole role)> GetUserRoleSelectionAsync(IEnumerable<DiscordRole> discordRoles, DiscordGuild guild, DiscordChannel channel, string interactionDropdownId, string messageContent = "Roles")
        {
            var cancellation = new CancellationTokenSource();

            var roleOptions = discordRoles.Select(CreateSelectComponentOption);
            var roleDropdown = new DiscordSelectComponent(interactionDropdownId, null, roleOptions);

            var messageBuilder = new DiscordMessageBuilder().WithContent(messageContent).AddComponents(roleDropdown);

            var message = await messageBuilder.SendAsync(channel);

            var chosenOption = await _interactivityExtension.WaitForSelectAsync(message, interactionDropdownId, cancellation.Token);

            await PostInteraction(chosenOption, message);

            var chosenRole = discordRoles.First(r => r.Id.ToString() == chosenOption.Result.Values[0]);

            var user = await guild.GetMemberAsync(chosenOption.Result.User.Id);
            return (user, chosenRole);
        }

        private async Task PostInteraction(InteractivityResult<ComponentInteractionCreateEventArgs> interactivityResult, DiscordMessage message)
        {
            if (interactivityResult.TimedOut)
            {
                await message.ModifyAsync(b =>
                {
                    b.Clear();
                    b.ClearComponents();
                });
            }
            else
            {
                await message.DeleteAsync();
            }
        }

        private static DiscordSelectComponentOption CreateSelectComponentOption(DiscordRole role)
            => new(role.Name, role.Id.ToString(), null, false);

        private async Task<DiscordRole> GetBotRoleAsync(DiscordUser botUser, DiscordGuild guild) 
            => await _roleManagementService.GetBotRoleAsync(botUser, guild);

        private IEnumerable<DiscordRole> GetAssignableRoles(DiscordRole botRole, DiscordGuild guild) =>
            // Get all roles that are lower than the bot's role (roles the bot can assign)
            _roleManagementService.GetAssignableRoles(botRole, guild);
    }
}