using Discord;
using Discord.Commands;
using dokas.FluentStrings;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
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
    public class RoleButtonsModule : ModuleBase
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
            var msg = await Context.Channel.GetMessageAsync(messageId);
            if (msg == null)
            {
                await ReplyAsync("Message not found. Make sure the message Id is correct");
                return;
            }
            var role = Context.Guild.Roles.SingleOrDefault(x => x.Name.ToLowerInvariant() == roleName.ToLowerInvariant());
            if (role == null)
            {
                await ReplyAsync("Role not found. Make sure the Rolename is correct");
                return;
            }
            IEmote emote = Context.Guild.Emotes.FirstOrDefault(x => emoteString.Contains(x.Name)) ?? new Emoji(emoteString) as IEmote;
            if (emote == null)
            {
                await ReplyAsync("Emote not found.");
                return;
            }
            if (await roleButtonService.ExistsAsync(Context.Guild.Id, messageId, role.Id))
            {
                await ReplyAsync("The specified link already exists");
                return;
            }
            await roleButtonService.AddRoleButtonLinkAsync(Context.Guild.Id, messageId, role.Id, emoteString);
        }

        [Command("RemoveLink")]
        [Remarks("Removes a reaction from the specified message with a link to the specified role")]
        public async Task RemoveLinkAsync([Summary("Id of the message to remove the link from")] ulong messageId, [Summary("Name of the role to remove the link from")] string roleName)
        {
            var msg = await Context.Channel.GetMessageAsync(messageId);
            if (msg == null)
            {
                await ReplyAsync("Message not found. Make sure the message Id is correct");
                return;
            }
            var role = Context.Guild.Roles.SingleOrDefault(x => x.Name.ToLowerInvariant() == roleName.ToLowerInvariant());
            if (role == null)
            {
                await ReplyAsync("Role not found. Make sure the Rolename is correct");
                return;
            }
            if (!(await roleButtonService.ExistsAsync(Context.Guild.Id, messageId, role.Id)))
            {
                await ReplyAsync("The specified link does not exist");
                return;
            }
            await roleButtonService.RemoveRoleButtonLinkAsync(Context.Guild.Id, messageId, role.Id);
        }

        [Command("RemoveAll")]
        [Remarks("Removes all Role Button Links")]
        public async Task RemoveAllAsync()
        {
            await roleButtonService.RemoveAllRoleButtonLinksAsync(Context.Guild.Id);
            await ReplyAsync("Role Button Links removed");
        }

        [Command("List")]
        [Remarks("Lists all Role Button Links")]
        public async Task ListAsync()
        {
            string links = await roleButtonService.ListAllAsync(Context.Guild.Id);
            if (!links.IsEmpty().OrWhiteSpace())
                await ReplyAsync(links);
            else
                await ReplyAsync("No role button links set up yet");
        }
    }
}