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
    /// Commands to modify Role-Dropdown-Links
    /// </summary>
    [Description("Role Selection management")]
    [MinPermissions(AccessLevel.ServerAdmin)]
    [RequireBotPermissions(Permissions.AddReactions | Permissions.ManageRoles | Permissions.ManageMessages)]
    public class RoleDropdownModule : BaseCommandModule
    {
        private readonly IRoleDropdownService _roleButtonService;

        public RoleDropdownModule(IRoleDropdownService roleButtonService)
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

            if (await _roleButtonService.ExistsAsync(ctx.Guild.Id))
            {
                var link = await _roleButtonService.GetForGuildAsync(ctx.Guild.Id);
                if (ctx.Guild.Channels.TryGetValue(link.ChannelId, out var linkChannel)
                    && (await linkChannel.GetMessageAsync(link.MessageId)) is DiscordMessage linkMessage)
                {
                    await ctx.RespondAsync($"There is already a role selector link - [{linkMessage.Id}]({linkMessage.JumpLink})");
                }
                else
                {
                    await ctx.RespondAsync("The was already a role selector link configured in this guild.");
                }
                return;
            }

            try
            {
                await _roleButtonService.AddRoleSelectorComponentAsync(ctx.Guild.Id, ctx.Channel.Id, messageToUse.Id);
            }
            catch (Exception ex) when (ex is ArgumentException or MessageComponentLinkAlreadyExistsException)
            {
                await ctx.ErrorAsync($"Error while trying to add the role selector component: {ex}");
            } 
        }

        [Command("RemoveRoleSelectorLink")]
        [Description("Removes role selector dropdowns for this guild")]
        public async Task RemoveRoleSelectorLinkAsync(CommandContext ctx)
        {
            if (!(await _roleButtonService.ExistsAsync(ctx.Guild.Id)))
            {
                await ctx.RespondAsync("There is no role dropdown configured for this guild");
                return;
            }

            try
            {
                await _roleButtonService.RemoveRoleSelectorComponentsAsync(ctx.Guild.Id);
            }
            catch (Exception ex) when (ex is ArgumentException or MessageComponentLinkNotFoundException)
            {
                await ctx.ErrorAsync($"Error while trying to remove the role selector component: {ex}");
            }
            
        }

        [Command("GetRoleLink")]
        [Description("Displays an existing role link, if configured")]
        public async Task ListAsync(CommandContext ctx)
        {
            var link = await _roleButtonService.GetForGuildAsync(ctx.Guild.Id);
            if (link != null)
            {
                if (!ctx.Guild.Channels.TryGetValue(link.ChannelId, out var linkChannel))
                {
                    await ctx.ErrorAsync("Found a link, but the channel was deleted in the meantime!");
                    return;
                }
                if ((await linkChannel.GetMessageAsync(link.MessageId)) is not DiscordMessage linkMessage)
                {
                    await ctx.ErrorAsync("Found a link, but the message was deleted in the meantime!");
                    return;
                }
                await ctx.OkAsync($"Link found - [{linkMessage.Id}]({linkMessage.JumpLink})");
            }
            else
            {
                await ctx.ErrorAsync("No role button links set up yet");
            }
        }
    }
}