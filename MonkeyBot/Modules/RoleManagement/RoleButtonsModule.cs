using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MonkeyBot.Common;
using MonkeyBot.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    /// <summary>
    /// Commands to modify Role-Button-Links
    /// </summary>
    [Description("Role Buttons management")]
    [MinPermissions(AccessLevel.ServerAdmin)]
    [RequireBotPermissions(Permissions.AddReactions | Permissions.ManageRoles | Permissions.ManageMessages)]
    public class RoleButtonsModule : BaseCommandModule
    {
        private readonly IRoleButtonService _roleButtonService;
        private readonly IRoleManagementService _roleManagementService;

        public const string AssignableRoleDropDownId = "assignableRoles-";

        public RoleButtonsModule(IRoleButtonService roleButtonService, IRoleManagementService roleManagementService)
        {
            _roleButtonService = roleButtonService;
            _roleManagementService = roleManagementService;
        }

        [Command("AddRoleSelectorLink")]
        [Description("Adds a dropdown to the specified message to select any role")]
        public async Task AddRoleSelectorLinkAsync(CommandContext ctx, [Description("Message to set up the dropdown for")] DiscordMessage message)
        {
            if (message == null)
            {
                await ctx.ErrorAsync("Message not found.");
                return;
            }

            if (await _roleButtonService.ExistsAsync(ctx.Guild.Id, ctx.Channel.Id, message.Id))
            {
                await ctx.RespondAsync("The specified link already exists");
                return;
            }

            var botRole = await _roleManagementService.GetBotRoleAsync(ctx.Client.CurrentUser, ctx.Guild);
            var assignableRoles = _roleManagementService.GetAssignableRoles(botRole, ctx.Guild);

            var roleOptions = assignableRoles.Select(CreateSelectComponentOption);
            var interactionDropdownId = AssignableRoleDropDownId + message.Id;
            var roleDropdown = new DiscordSelectComponent(interactionDropdownId, null, roleOptions);

            await _roleButtonService.AddRoleSelectorComponentAsync(ctx.Guild.Id, ctx.Channel.Id, message.Id, roleDropdown);
        }

        [Command("AddRoleLink")]
        [Description("Adds a reaction to the specified message with a link to the specified role")]
        public async Task AddLinkAsync(CommandContext ctx, [Description("Message to set up the link for")] DiscordMessage message, [Description("Role to link")] DiscordRole role, [Description("Emoji to link")] DiscordEmoji emoji)
        {

            if (message == null)
            {
                await ctx.ErrorAsync("Message not found.");
                return;
            }

            if (role == null)
            {
                await ctx.ErrorAsync("Role not found.");
                return;
            }

            if (emoji == null)
            {
                await ctx.RespondAsync("Emoji not found.");
                return;
            }
            if (await _roleButtonService.ExistsAsync(ctx.Guild.Id, ctx.Channel.Id, message.Id, role.Id))
            {
                await ctx.RespondAsync("The specified link already exists");
                return;
            }
            await _roleButtonService.AddRoleButtonLinkAsync(ctx.Guild.Id, ctx.Channel.Id, message.Id, role.Id, emoji.ToString());
            await ctx.OkAsync($"Role {role.Name} successfully linked. Press the {emoji} Reaction on the linked message to get the role");
        }

        [Command("RemoveRoleLink")]
        [Description("Removes a reaction from the specified message with a link to the specified role")]
        public async Task RemoveLinkAsync(CommandContext ctx, [Description("Message to remove the link from")] DiscordMessage message, [Description("Role to remove the link from")] DiscordRole role)
        {
            if (message == null)
            {
                await ctx.ErrorAsync("Message not found.");
                return;
            }

            if (role == null)
            {
                await ctx.ErrorAsync("Role not found.");
                return;
            }
            if (!(await _roleButtonService.ExistsAsync(ctx.Guild.Id, ctx.Channel.Id, message.Id, role.Id)))
            {
                await ctx.ErrorAsync("The specified link does not exist");
                return;
            }
            await _roleButtonService.RemoveRoleButtonLinkAsync(ctx.Guild.Id, ctx.Channel.Id, message.Id, role.Id);
            await ctx.OkAsync($"Role {role.Name} successfully unlinked.");
        }

        [Command("RemoveAllRoleLinks")]
        [Description("Removes all Role Button Links")]
        public async Task RemoveAllAsync(CommandContext ctx)
        {
            await _roleButtonService.RemoveAllRoleButtonLinksAsync(ctx.Guild.Id);
            await ctx.OkAsync("All Role Button Links removed");
        }

        [Command("ListRoleLinks")]
        [Description("Lists all Role Button Links")]
        public async Task ListAsync(CommandContext ctx)
        {
            string links = await _roleButtonService.ListAllAsync(ctx.Guild.Id);
            _ = !links.IsEmptyOrWhiteSpace()
                ? await ctx.OkAsync(links, "Role links")
                : await ctx.ErrorAsync("No role button links set up yet");
        }

        private DiscordSelectComponentOption CreateSelectComponentOption(DiscordRole role)
            => new(role.Name, role.Id.ToString(), null, false);
    }
}