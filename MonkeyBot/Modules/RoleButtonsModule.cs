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
    [Name("Role Buttons")]
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
        [Remarks("Adds a reaction to the specified message with a link to the specified role")]
        public async Task AddLinkAsync([Summary("Id of the message to set up the link for")] ulong messageId, [Summary("Name of the role to link")] string roleName, [Summary("Emote to link")] string emoteString)
        {
            var msg = await Context.Channel.GetMessageAsync(messageId).ConfigureAwait(false);
            if (msg == null)
            {
                await ReplyAsync("Message not found. Make sure the message Id is correct").ConfigureAwait(false);
                return;
            }
            IRole role = await GetRoleInGuildAsync(roleName).ConfigureAwait(false);
            if (role == null)
                return;
            IEmote emote = Context.Guild.Emotes.FirstOrDefault(x => emoteString.Contains(x.Name, StringComparison.OrdinalIgnoreCase)) ?? new Emoji(emoteString) as IEmote;
            if (emote == null)
            {
                await ReplyAsync("Emote not found.").ConfigureAwait(false);
                return;
            }
            if (await roleButtonService.ExistsAsync(Context.Guild.Id, messageId, role.Id).ConfigureAwait(false))
            {
                await ReplyAsync("The specified link already exists").ConfigureAwait(false);
                return;
            }
            await roleButtonService.AddRoleButtonLinkAsync(Context.Guild.Id, messageId, role.Id, emoteString).ConfigureAwait(false);
        }

        [Command("RemoveLink")]
        [Remarks("Removes a reaction from the specified message with a link to the specified role")]
        public async Task RemoveLinkAsync([Summary("Id of the message to remove the link from")] ulong messageId, [Summary("Name of the role to remove the link from")] string roleName)
        {
            var msg = await Context.Channel.GetMessageAsync(messageId).ConfigureAwait(false);
            if (msg == null)
            {
                await ReplyAsync("Message not found. Make sure the message Id is correct").ConfigureAwait(false);
                return;
            }
            IRole role = await GetRoleInGuildAsync(roleName).ConfigureAwait(false);
            if (role == null)
                return;
            if (!(await roleButtonService.ExistsAsync(Context.Guild.Id, messageId, role.Id).ConfigureAwait(false)))
            {
                await ReplyAsync("The specified link does not exist").ConfigureAwait(false);
                return;
            }
            await roleButtonService.RemoveRoleButtonLinkAsync(Context.Guild.Id, messageId, role.Id).ConfigureAwait(false);
        }

        [Command("RemoveAll")]
        [Remarks("Removes all Role Button Links")]
        public async Task RemoveAllAsync()
        {
            await roleButtonService.RemoveAllRoleButtonLinksAsync(Context.Guild.Id).ConfigureAwait(false);
            await ReplyAsync("Role Button Links removed").ConfigureAwait(false);
        }

        [Command("List")]
        [Remarks("Lists all Role Button Links")]
        public async Task ListAsync()
        {
            string links = await roleButtonService.ListAllAsync(Context.Guild.Id).ConfigureAwait(false);
            if (!links.IsEmptyOrWhiteSpace())
                await ReplyAsync(links).ConfigureAwait(false);
            else
                await ReplyAsync("No role button links set up yet").ConfigureAwait(false);
        }
    }
}