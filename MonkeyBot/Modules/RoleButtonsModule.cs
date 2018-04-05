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
    [MinPermissions(AccessLevel.ServerAdmin)]
    [Name("Role Buttons")]
    [Group("RoleButtons")]
    public class RoleButtonsModule : ModuleBase
    {
        private readonly IRoleButtonService roleButtonService;

        public RoleButtonsModule(IRoleButtonService roleButtonService)
        {
            this.roleButtonService = roleButtonService;
        }

        [Command("AddLink")]
        [Remarks("Adds a reaction to the specified message with a link to the specified role")]
        public async Task AddLinkAsync(ulong messageId, string roleName, string emoji)
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
            await roleButtonService.AddRoleButtonLinkAsync(Context.Guild.Id, messageId, role.Id, emoji);
        }

        [Command("RemoveLink")]
        [Remarks("Removes a reaction from the specified message with a link to the specified role")]
        public async Task RemoveLinkAsync(ulong messageId, string roleName)
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
            await roleButtonService.RemoveRoleButtonLinkAsync(Context.Guild.Id, messageId, role.Id);
        }

        [Command("RemoveAll")]
        [Remarks("Removes all Role Button Links")]
        public async Task RemoveAllAsync()
        {
            await roleButtonService.RemoveAllRoleButtonLinksAsync(Context.Guild.Id);
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