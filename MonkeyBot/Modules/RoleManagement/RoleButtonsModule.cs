using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MonkeyBot.Common;
using MonkeyBot.Services;
using System;
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