using Discord;
using Discord.Commands;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    /// <summary>
    /// Commands to modify Role-Button-Links
    /// </summary>
    [Description("Role Buttons")]
    [Group("RoleButtons")]
    [MinPermissions(AccessLevel.ServerAdmin)]
    [RequireBotPermission(GuildPermission.AddReactions | GuildPermission.ManageRoles | GuildPermission.ManageMessages)]
    public class RoleButtonsModule : MonkeyModuleBase
    {
        private readonly IRoleButtonService roleButtonService;

        public RoleButtonsModule(IRoleButtonService roleButtonService)
        {
            this.roleButtonService = roleButtonService;
        }

        [Command("AddLink")]
        [Description("Adds a reaction to the specified message with a link to the specified role")]
        public async Task AddLinkAsync([Summary("Id of the message to set up the link for")] ulong messageId, [Summary("Name of the role to link")] string roleName, [Summary("Emote to link")] string emoteString)
        {
            IMessage msg = await Context.Channel.GetMessageAsync(messageId).ConfigureAwait(false);
            if (msg == null)
            {
                _ = await ctx.RespondAsync("Message not found. Make sure the message Id is correct").ConfigureAwait(false);
                return;
            }
            IRole role = await GetRoleInGuildAsync(roleName).ConfigureAwait(false);
            if (role == null)
            {
                return;
            }
            IEmote emote = Context.Guild.Emotes.FirstOrDefault(x => emoteString.Contains(x.Name, StringComparison.OrdinalIgnoreCase)) ?? new Emoji(emoteString) as IEmote;
            if (emote == null)
            {
                _ = await ctx.RespondAsync("Emote not found.").ConfigureAwait(false);
                return;
            }
            if (await roleButtonService.ExistsAsync(Context.Guild.Id, messageId, role.Id).ConfigureAwait(false))
            {
                _ = await ctx.RespondAsync("The specified link already exists").ConfigureAwait(false);
                return;
            }
            await roleButtonService.AddRoleButtonLinkAsync(Context.Guild.Id, messageId, role.Id, emoteString).ConfigureAwait(false);
        }

        [Command("RemoveLink")]
        [Description("Removes a reaction from the specified message with a link to the specified role")]
        public async Task RemoveLinkAsync([Summary("Id of the message to remove the link from")] ulong messageId, [Summary("Name of the role to remove the link from")] string roleName)
        {
            IMessage msg = await Context.Channel.GetMessageAsync(messageId).ConfigureAwait(false);
            if (msg == null)
            {
                _ = await ctx.RespondAsync("Message not found. Make sure the message Id is correct").ConfigureAwait(false);
                return;
            }
            IRole role = await GetRoleInGuildAsync(roleName).ConfigureAwait(false);
            if (role == null)
            {
                return;
            }
            if (!(await roleButtonService.ExistsAsync(Context.Guild.Id, messageId, role.Id).ConfigureAwait(false)))
            {
                _ = await ctx.RespondAsync("The specified link does not exist").ConfigureAwait(false);
                return;
            }
            await roleButtonService.RemoveRoleButtonLinkAsync(Context.Guild.Id, messageId, role.Id).ConfigureAwait(false);
        }

        [Command("RemoveAll")]
        [Description("Removes all Role Button Links")]
        public async Task RemoveAllAsync()
        {
            await roleButtonService.RemoveAllRoleButtonLinksAsync(Context.Guild.Id).ConfigureAwait(false);
            _ = await ctx.RespondAsync("Role Button Links removed").ConfigureAwait(false);
        }

        [Command("List")]
        [Description("Lists all Role Button Links")]
        public async Task ListAsync()
        {
            string links = await roleButtonService.ListAllAsync(Context.Guild.Id).ConfigureAwait(false);
            _ = !links.IsEmptyOrWhiteSpace()
                ? await ctx.RespondAsync(links).ConfigureAwait(false)
                : await ctx.RespondAsync("No role button links set up yet").ConfigureAwait(false);
        }
    }
}