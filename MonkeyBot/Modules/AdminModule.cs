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
    public class AdminModule : ModuleBase
    {
        [Command("AddOwner")]
        [Remarks("Adds the specified user to the list of bot owners")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task AddOwner([Summary("The name of the user to add")] string username)
        {
            var match = (await Context.Guild.GetUsersAsync()).Where(x => x.Username == username);
            if (match == null || match.Count() <= 0)
                await ReplyAsync("User not found!");
            else if (match.Count() > 1)
                await ReplyAsync("Multiple users found, please be more specific!");
            else
            {
                var userToAdd = match.First();
                var config = await Configuration.LoadAsync();
                var owners = config.Owners.ToList();
                if (!owners.Contains(userToAdd.Id))
                {
                    owners.Add(userToAdd.Id);
                    config.Owners = owners.ToArray();
                    await config.SaveJsonAsync();
                    await ReplyAsync($"{userToAdd.Username} has been added to the list of bot owners!");
                }
                else
                    await ReplyAsync($"{userToAdd.Username} already is a bot owner!");
            }
        }

        [Command("RemoveOwner")]
        [Remarks("Removes the specified user from the list of bot owners")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task RemoveOwner([Summary("The name of the user to remove")] string username)
        {
            var match = (await Context.Guild.GetUsersAsync()).Where(x => x.Username == username);
            if (match == null || match.Count() <= 0)
                await ReplyAsync("User not found!");
            else if (match.Count() > 1)
                await ReplyAsync("Multiple users found, please be more specific!");
            else
            {
                var userToRemove = match.First();
                var config = await Configuration.LoadAsync();
                var owners = config.Owners.ToList();
                if (owners.Contains(userToRemove.Id))
                {
                    owners.Remove(userToRemove.Id);
                    config.Owners = owners.ToArray();
                    await config.SaveJsonAsync();
                    await ReplyAsync($"{userToRemove.Username} has been removed from the list of bot owners!");
                }
                else
                    await ReplyAsync($"{userToRemove.Username} is not a bot owner!");
            }
        }
    }
}