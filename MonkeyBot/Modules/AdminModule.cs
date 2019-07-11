using Discord;
using Discord.Commands;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    /// <summary>
    /// Provides admin level commands
    /// </summary>
    [MinPermissions(AccessLevel.ServerAdmin)]
    [Name("Admin Commands")]
    public class AdminModule : MonkeyModuleBase
    {
        [Command("AddOwner")]
        [Remarks("Adds the specified user to the list of bot owners")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task AddOwnerAsync([Summary("The name of the user to add")] string username)
        {            
            IGuildUser user = await GetUserInGuildAsync(username);
            if (user == null)
                return;
            var config = await DiscordClientConfiguration.LoadAsync();           
            if (!config.Owners.Contains(user.Id))
            {
                config.AddOwner(user.Id);                
                await config.SaveAsync();
                await ReplyAsync($"{user.Username} has been added to the list of bot owners!");
            }
            else
                await ReplyAsync($"{user.Username} already is a bot owner!");
        }

        

        [Command("RemoveOwner")]
        [Remarks("Removes the specified user from the list of bot owners")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task RemoveOwnerAsync([Summary("The name of the user to remove")] string username)
        {
            IGuildUser user = await GetUserInGuildAsync(username);
            if (user == null)
                return;
            var config = await DiscordClientConfiguration.LoadAsync();            
            if (config.Owners.Contains(user.Id))
            {
                config.RemoveOwner(user.Id);
                await config.SaveAsync();
                await ReplyAsync($"{user.Username} has been removed from the list of bot owners!");
            }
            else
                await ReplyAsync($"{user.Username} is not a bot owner!");
            
        }
    }
}