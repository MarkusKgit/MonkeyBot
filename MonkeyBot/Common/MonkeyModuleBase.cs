using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Common
{
    public abstract class MonkeyModuleBase : ModuleBase
    {
        protected async Task ReplyAndDeleteAsync(string message = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, int delayMS = 5000)
        {
            IUserMessage msg = await Context.Channel.SendMessageAsync(message, isTTS, embed, options).ConfigureAwait(false);
            await Task.Delay(delayMS).ConfigureAwait(false);
            await Context.Channel.DeleteMessageAsync(msg).ConfigureAwait(false);
            await Context.Channel.DeleteMessageAsync(Context.Message).ConfigureAwait(false);
        }

        protected async Task<IGuildUser> GetUserInGuildAsync(string userName)
        {
            if (userName.IsEmptyOrWhiteSpace())
            {
                await ReplyAsync("Please provide a name").ConfigureAwait(false);
                return null;
            }
            IGuildUser user = null;
            if (userName.StartsWith("<@", StringComparison.InvariantCulture) && ulong.TryParse(userName.Replace("<@", "", StringComparison.InvariantCulture).Replace(">", "", StringComparison.InvariantCulture), out ulong id))
            {
                user = await Context.Guild.GetUserAsync(id).ConfigureAwait(false);
            }
            else
            {
                var users = (await (Context.Guild?.GetUsersAsync()).ConfigureAwait(false))?.Where(x => x.Username.Contains(userName, StringComparison.OrdinalIgnoreCase));
                if (users != null && users.Count() == 1)
                    user = users.First();
                else if (users == null)
                    await ReplyAsync("User not found").ConfigureAwait(false);
                else
                    await ReplyAsync("Multiple users found! Please be more specific. Did you mean one of the following:"
                        + Environment.NewLine
                        + string.Join(", ", users.Select(x => x.Username))
                        ).ConfigureAwait(false);
            }
            return user;
        }

        protected async Task<ITextChannel> GetTextChannelInGuildAsync(string channelName, bool defaultToCurrent)
        {
            if (channelName.IsEmptyOrWhiteSpace() && !defaultToCurrent)
            {
                await ReplyAsync("Please provide the name of the channel").ConfigureAwait(false);
                return null;
            }
            var allChannels = await Context.Guild.GetTextChannelsAsync().ConfigureAwait(false);
            ITextChannel channel = null;
            if (!channelName.IsEmpty())
            {
                channel = allChannels.FirstOrDefault(x => x.Name.Equals(channelName, StringComparison.OrdinalIgnoreCase));
            }
            else if (defaultToCurrent)
            {
                channel = Context.Channel as ITextChannel;
            }
            if (channel == null)
                await ReplyAsync("The specified channel does not exist").ConfigureAwait(false);
            return channel;
        }

        protected async Task<IRole> GetRoleInGuildAsync(string roleName)
        {
            if (roleName.IsEmptyOrWhiteSpace())
            {
                await ReplyAsync("Please provide the name of the role").ConfigureAwait(false);
                return null;
            }
            IRole role = Context.Guild.Roles.FirstOrDefault(x => x.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
            if (role == null)
            {
                await ReplyAsync("The role you specified is invalid!").ConfigureAwait(false);
            }
            return role;
        }

        /// <summary>Get the bot's highest ranked role with permission Manage Roles</summary>
        public async Task<IRole> GetManageRolesRoleAsync()
        {
            IGuildUser thisBot = await Context.Guild.GetUserAsync(Context.Client.CurrentUser.Id).ConfigureAwait(false);
            IRole ownrole = Context.Guild.Roles.FirstOrDefault(x => x.Permissions.ManageRoles && x.Id == thisBot.RoleIds.Max());
            return ownrole;
        }
    }
}