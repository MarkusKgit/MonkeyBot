using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MonkeyBot.Common
{
    public static class MonkeyHelpers
    {
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
                if (!strippedPath.IsEmpty() && !Directory.Exists(strippedPath))
                {
                    _ = Directory.CreateDirectory(strippedPath);
                }
            }

            using var streamWriter = new StreamWriter(filePath, append);
            await streamWriter.WriteAsync(text);
        }

        /// <summary>
        /// Reads the contents of the specified textfile to a string asynchronously
        /// </summary>
        /// <param name="filePath">Path of the file to read</param>
        /// <returns>Contents of the text file</returns>
        public static async Task<string> ReadTextAsync(string filePath)
        {
            using var streamReader = new StreamReader(filePath);
            return await streamReader.ReadToEndAsync();
        }

        public static async Task<T> WithCancellationAsync<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using CancellationTokenRegistration _ = cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs);
            if (task != await Task.WhenAny(task, tcs.Task))
            {
                throw new OperationCanceledException(cancellationToken);
            }
            return await task;
        }

        //Converts all html encoded special characters
        public static string CleanHtmlString(string html)
            => System.Net.WebUtility.HtmlDecode(html);

        private static readonly string[] regionalIndicatorLetters = "🇦|🇧|🇨|🇩|🇪|🇫|🇬|🇭|🇮|🇯|🇰|🇱|🇲|🇳|🇴|🇵|🇶|🇷|🇸|🇹|🇺|🇻|🇼|🇽|🇾|🇿|".Split('|');

        public static string GetUnicodeRegionalLetter(int index)
        {
            if (index < 0 || index >= regionalIndicatorLetters.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return regionalIndicatorLetters[index];
        }

        internal static Task<DiscordMessage> SendChannelMessageAsync(DiscordClient discordClient, ulong guildID, ulong channelID, string message = null, DiscordEmbed embed = null)
        {
            if (discordClient.Guilds.TryGetValue(guildID, out DiscordGuild guild))
            {
                if (guild.Channels.TryGetValue(channelID, out DiscordChannel channel))
                {
                    return channel.SendMessageAsync(content: message, embed: embed);
                }
            }
            return Task.FromResult<DiscordMessage>(null);
        }
    }
}