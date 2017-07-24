using Discord;
using Discord.Commands;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyBot.Common
{
    public static class Helpers
    {
        /// <summary>Get the role of the bot with permission Manage Roles</summary>
        public static async Task<IRole> GetBotRoleAsync(ICommandContext context)
        {
            var thisBot = await context.Guild.GetUserAsync(context.Client.CurrentUser.Id);
            var ownrole = context.Guild.Roles.Where(x => x.Permissions.ManageRoles == true && x.Id == thisBot.RoleIds.Max()).FirstOrDefault();
            return ownrole;
        }

        /// <summary>Writes the specified string to a textfile asynchronously</summary>
        public static async Task WriteTextAsync(string filePath, string text, bool append = false)
        {
            byte[] encodedText = Encoding.Unicode.GetBytes(text);

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

        /// <summary>Reads the contents of the specified textfile to a string asynchronously</summary>
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
                    string text = Encoding.Unicode.GetString(buffer, 0, numRead);
                    sb.Append(text);
                }
                return sb.ToString();
            }
        }
    }
}