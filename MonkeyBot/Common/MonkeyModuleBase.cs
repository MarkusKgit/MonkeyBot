using Discord;
using Discord.Commands;
using dokas.FluentStrings;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Common
{
    public abstract class MonkeyModuleBase : ModuleBase
    {
        protected async Task ReplyAndDeleteAsync(string message = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, int delayMS = 5000)
        {
            var msg = await Context.Channel.SendMessageAsync(message, isTTS, embed, options).ConfigureAwait(false);
            await Task.Delay(delayMS);
            await Context.Channel.DeleteMessageAsync(msg);
            await Context.Channel.DeleteMessageAsync(Context.Message);
        }

        protected async Task<IGuildUser> GetUserInGuildAsync(string userName)
        {
            if (userName.IsEmpty().OrWhiteSpace())
            {
                await ReplyAsync("Please provide a name");
                return null;
            }
            IGuildUser user = null;
            if (userName.StartsWith("<@") && ulong.TryParse(userName.Replace("<@", "").Replace(">", ""), out var id))
                user = await Context.Guild.GetUserAsync(id);
            else
            {
                var users = (await Context.Guild?.GetUsersAsync())?.Where(x => x.Username.ToLower().Contains(userName.ToLower()));
                if (users != null && users.Count() == 1)
                    user = users.First();
                else if (users == null)
                    await ReplyAsync("User not found");
                else
                    await ReplyAsync("Multiple users found! Please be more specific. Did you mean one of the following:"
                        + Environment.NewLine
                        + string.Join(", ", users.Select(x => x.Username))
                        );
            }
            return null;
        }

        protected async Task<ITextChannel> GetTextChannelInGuildAsync(string channelName, bool defaultToCurrent)
        {
            if (channelName.IsEmpty().OrWhiteSpace() && !defaultToCurrent)
            {
                await ReplyAsync("Please provide the name of the channel");
                return null;
            }
            var allChannels = await Context.Guild.GetTextChannelsAsync();
            ITextChannel channel = null;
            if (!channelName.IsEmpty())
            {
                channel = allChannels.FirstOrDefault(x => x.Name.ToLower() == channelName.ToLower());
            }
            else if (defaultToCurrent)
            {
                channel = Context.Channel as ITextChannel;
            }
            if (channel == null)
                await ReplyAsync("The specified channel does not exist");
            return channel;
        }

        protected async Task<IRole> GetRoleInGuildAsync(string roleName)
        {
            if (roleName.IsEmpty().OrWhiteSpace())
            {
                await ReplyAsync("Please provide the name of the role");
                return null;
            }
            IRole role = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == roleName.ToLower());
            if (role == null)
            {
                await ReplyAsync("The role you specified is invalid!");
            }
            return role;
        }
    }
}