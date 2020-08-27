using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.IO;
using System.Text;
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

            byte[] encodedText = Encoding.UTF8.GetBytes(text);

            using var sourceStream = new FileStream(filePath,
                                                     append ? FileMode.Append : FileMode.Create,
                                                     FileAccess.Write,
                                                     FileShare.None,
                                                     bufferSize: 4096,
                                                     useAsync: true);
            await sourceStream.WriteAsync(encodedText, 0, encodedText.Length).ConfigureAwait(false);
        }

        /// <summary>
        /// Reads the contents of the specified textfile to a string asynchronously
        /// </summary>
        /// <param name="filePath">Path of the file to read</param>
        /// <returns>Contents of the text file</returns>
        public static async Task<string> ReadTextAsync(string filePath)
        {
            using var sourceStream = new FileStream(filePath,
                                                    FileMode.Open,
                                                    FileAccess.Read,
                                                    FileShare.Read,
                                                    bufferSize: 4096,
                                                    useAsync: true);
            var sb = new StringBuilder();

            byte[] buffer = new byte[0x1000];
            int numRead;
            while ((numRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) != 0)
            {
                string text = Encoding.UTF8.GetString(buffer, 0, numRead);
                _ = sb.Append(text);
            }
            return sb.ToString();
        }

        public static async Task<T> WithCancellationAsync<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using CancellationTokenRegistration _ = cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs);
            if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(false))
            {
                throw new OperationCanceledException(cancellationToken);
            }
            return await task.ConfigureAwait(false);
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

        internal static Task SendChannelMessageAsync(DiscordClient discordClient, ulong guildID, ulong channelID, string message)
        {
            if (discordClient.Guilds.TryGetValue(guildID, out DiscordGuild guild))
            {
                if (guild.Channels.TryGetValue(channelID, out var channel))
                {
                    return channel.SendMessageAsync(message);
                }
            }
            return Task.CompletedTask;
        }
    }
}