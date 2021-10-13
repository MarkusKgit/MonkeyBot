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

        public RoleButtonsModule(IRoleButtonService roleButtonService)
        {
            _roleButtonService = roleButtonService;
        }

        [Command("AddRoleSelectorLink")]
        [Description("Adds a dropdown to a message to select any role")]
        public async Task AddRoleSelectorLinkAsync(CommandContext ctx, [Description("Message to set up the dropdown for")] DiscordMessage message = null)
        {
            var messageToUse = message ?? ctx.Message.ReferencedMessage;
            if (messageToUse == null)
            {
                await ctx.ErrorAsync("Message not found. Please either reply to the message you want to set up the dropdown for or provide the message id as a parameter");
                return;
            }

            if (await _roleButtonService.ExistsAsync(ctx.Guild.Id, ctx.Channel.Id, messageToUse.Id))
            {
                await ctx.RespondAsync("The specified link already exists");
                return;
            }

            await _roleButtonService.AddRoleSelectorComponentAsync(ctx.Guild.Id, ctx.Channel.Id, messageToUse.Id, ctx.Client.CurrentUser);
        }

        [Command("RemoveRoleSelectorLink")]
        [Description("Removes role selector dropdowns from a message")]
        public async Task RemoveRoleSelectorLinkAsync(CommandContext ctx, [Description("Message to remove the link from")] DiscordMessage message = null)
        {
            var messageToUse = message ?? ctx.Message.ReferencedMessage;
            if (messageToUse == null)
            {
                await ctx.ErrorAsync("Message not found. Please either reply to the message you want to set up the dropdown for or provide the message id as a parameter");
                return;
            }

            if (!(await _roleButtonService.ExistsAsync(ctx.Guild.Id, ctx.Channel.Id, messageToUse.Id)))
            {
                await ctx.RespondAsync("The specified link does not exist");
                return;
            }

            await _roleButtonService.RemoveRoleSelectorComponentsAsync(ctx.Guild.Id, ctx.Channel.Id, messageToUse.Id);
        }

        [Command("RemoveAllRoleSelectorLinks")]
        [Description("Removes all role selector dropdowns from messages")]
        public async Task RemoveAllRoleSelectorLinkAsync(CommandContext ctx) => 
            await _roleButtonService.RemoveAllRoleSelectorComponentsAsync(ctx.Guild.Id);

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
    }
}