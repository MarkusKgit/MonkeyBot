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
        private readonly IRoleButtonService roleButtonService;

        public RoleButtonsModule(IRoleButtonService roleButtonService)
        {
            this.roleButtonService = roleButtonService;
        }

        [Command("AddRoleLink")]
        [Description("Adds a reaction to the specified message with a link to the specified role")]
        public async Task AddLinkAsync(CommandContext ctx, [Description("Message to set up the link for")] DiscordMessage message, [Description("Role to link")] DiscordRole role, [Description("Emoji to link")] DiscordEmoji emoji)
        {

            if (message == null)
            {
                _ = await ctx.ErrorAsync("Message not found.").ConfigureAwait(false);
                return;
            }

            if (role == null)
            {
                _ = await ctx.ErrorAsync("Role not found.").ConfigureAwait(false);
                return;
            }

            if (emoji == null)
            {
                _ = await ctx.RespondAsync("Emoji not found.").ConfigureAwait(false);
                return;
            }
            if (await roleButtonService.ExistsAsync(ctx.Guild.Id, ctx.Channel.Id, message.Id, role.Id).ConfigureAwait(false))
            {
                _ = await ctx.RespondAsync("The specified link already exists").ConfigureAwait(false);
                return;
            }
            await roleButtonService.AddRoleButtonLinkAsync(ctx.Guild.Id, ctx.Channel.Id, message.Id, role.Id, emoji.ToString()).ConfigureAwait(false);
            _ = await ctx.OkAsync($"Role {role.Name} successfully linked. Press the {emoji} Reaction on the linked message to get the role").ConfigureAwait(false);
        }

        [Command("RemoveRoleLink")]
        [Description("Removes a reaction from the specified message with a link to the specified role")]
        public async Task RemoveLinkAsync(CommandContext ctx, [Description("Message to remove the link from")] DiscordMessage message, [Description("Role to remove the link from")] DiscordRole role)
        {
            if (message == null)
            {
                _ = await ctx.ErrorAsync("Message not found.").ConfigureAwait(false);
                return;
            }

            if (role == null)
            {
                _ = await ctx.ErrorAsync("Role not found.").ConfigureAwait(false);
                return;
            }
            if (!(await roleButtonService.ExistsAsync(ctx.Guild.Id, ctx.Channel.Id, message.Id, role.Id).ConfigureAwait(false)))
            {
                _ = await ctx.ErrorAsync("The specified link does not exist").ConfigureAwait(false);
                return;
            }
            await roleButtonService.RemoveRoleButtonLinkAsync(ctx.Guild.Id, ctx.Channel.Id, message.Id, role.Id).ConfigureAwait(false);
            _ = await ctx.OkAsync($"Role {role.Name} successfully unlinked.").ConfigureAwait(false);
        }

        [Command("RemoveAllRoleLinks")]
        [Description("Removes all Role Button Links")]
        public async Task RemoveAllAsync(CommandContext ctx)
        {
            await roleButtonService.RemoveAllRoleButtonLinksAsync(ctx.Guild.Id).ConfigureAwait(false);
            _ = await ctx.OkAsync("All Role Button Links removed").ConfigureAwait(false);
        }

        [Command("ListRoleLinks")]
        [Description("Lists all Role Button Links")]
        public async Task ListAsync(CommandContext ctx)
        {
            string links = await roleButtonService.ListAllAsync(ctx.Guild.Id).ConfigureAwait(false);
            _ = !links.IsEmptyOrWhiteSpace()
                ? await ctx.OkAsync(links, "Role links").ConfigureAwait(false)
                : await ctx.ErrorAsync("No role button links set up yet").ConfigureAwait(false);
        }
    }
}