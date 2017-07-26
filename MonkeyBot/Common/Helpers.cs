using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyBot.Common
{
    public static class Helpers
    {
        /// <summary>Get the bot's highest ranked role with permission Manage Roles</summary>
        public static async Task<IRole> GetManageRolesRoleAsync(ICommandContext context)
        {
            var thisBot = await context.Guild.GetUserAsync(context.Client.CurrentUser.Id);
            var ownrole = context.Guild.Roles.Where(x => x.Permissions.ManageRoles == true && x.Id == thisBot.RoleIds.Max()).FirstOrDefault();
            return ownrole;
        }

        /// <summary>
        /// Writes the specified string to a textfile asynchronously
        /// </summary>
        /// <param name="filePath">Path of the file to write</param>
        /// <param name="text">Text to write to the file</param>
        /// <param name="append">Appends the text to the existing file if true, overrides the file if false</param>
        public static async Task WriteTextAsync(string filePath, string text, bool append = false)
        {
            if (!File.Exists(filePath))
            {
                string strippedPath = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(strippedPath))
                    Directory.CreateDirectory(strippedPath);
            }

            byte[] encodedText = Encoding.UTF8.GetBytes(text);

            using (FileStream sourceStream = new FileStream(
                filePath,
                append ? FileMode.Append : FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true)
                )
            {
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            };
        }

        /// <summary>
        /// Reads the contents of the specified textfile to a string asynchronously
        /// </summary>
        /// <param name="filePath">Path of the file to read</param>
        /// <returns>Contents of the text file</returns>
        public static async Task<string> ReadTextAsync(string filePath)
        {
            using (FileStream sourceStream = new FileStream(filePath,
                FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 4096, useAsync: true))
            {
                StringBuilder sb = new StringBuilder();

                byte[] buffer = new byte[0x1000];
                int numRead;
                while ((numRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    string text = Encoding.UTF8.GetString(buffer, 0, numRead);
                    sb.Append(text);
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Sends the text to the specified guild's channel using a connected Discord client
        /// </summary>
        /// <param name="client">Connected Discord Client connection</param>
        /// <param name="guildID">Id of the Discord guild</param>
        /// <param name="channelID">Id of the Discord channel</param>
        /// <param name="text">Text to post</param>
        public static async Task SendChannelMessageAsync(IDiscordClient client, ulong guildID, ulong channelID, string text)
        {
            var guild = await client?.GetGuildAsync(guildID);
            var channel = await guild?.GetChannelAsync(channelID) as SocketTextChannel;
            await channel?.SendMessageAsync(text);
        }
    }
}