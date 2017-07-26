using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MonkeyBot.Common
{
    /// <summary>
    /// Contains configuration settings including token which should not be public
    /// </summary>
    public class Configuration
    {
        [JsonIgnore]
        /// <summary> The location and name of the bot's configuration file. </summary>
        public static string FileName { get; private set; } = "config/configuration.json";

        /// <summary> Ids of users who will have owner access to the bot. </summary>
        public ulong[] Owners { get; set; }

        /// <summary> The bot's command prefix. </summary>
        public string Prefix { get; set; } = "!";

        /// <summary> The bot's productive login token. </summary>
        public string ProductiveToken { get; set; } = "";

        /// <summary> The bot's testing login token. </summary>
        public string TestingToken { get; set; } = "";

        /// <summary>Makes sure that a config file exists and asks for the tokens on first run</summary>
        public static async Task EnsureExistsAsync()
        {
            string file = Path.Combine(AppContext.BaseDirectory, FileName);
            if (!File.Exists(file)) // Check if the configuration file exists.
            {
                string path = Path.GetDirectoryName(file); // Create config directory if doesn't exist.
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var config = new Configuration(); // Create a new configuration object.

                string token;
                await Console.Out.WriteLineAsync("Please enter your productive token: ");
                token = await Console.In.ReadLineAsync(); // Read the productive bot token from console.
                config.ProductiveToken = token;
                await Console.Out.WriteLineAsync("Please enter your testing token: ");
                token = await Console.In.ReadLineAsync(); // Read the testing bot token from console.
                config.TestingToken = token;
                config.Owners = new ulong[] { 327885109560737793 };

                await config.SaveJsonAsync(); // Save the new configuration object to file.
            }
            await Console.Out.WriteLineAsync("Configuration Loaded");
        }

        /// <summary> Save the configuration to the path specified in FileName. </summary>
        public async Task SaveJsonAsync()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, FileName);
            await Helpers.WriteTextAsync(filePath, ToJson());
        }

        /// <summary> Load the configuration from the path specified in FileName. </summary>
        public static async Task<Configuration> LoadAsync()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, FileName);
            string json = await Helpers.ReadTextAsync(filePath);
            return JsonConvert.DeserializeObject<Configuration>(json);
        }

        /// <summary> Convert the configuration to a json string. </summary>
        private string ToJson()
            => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}