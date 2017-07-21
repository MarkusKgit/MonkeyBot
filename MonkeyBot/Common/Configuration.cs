using Newtonsoft.Json;
using System;
using System.IO;

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
        public static void EnsureExists()
        {
            string file = Path.Combine(AppContext.BaseDirectory, FileName);
            if (!File.Exists(file)) // Check if the configuration file exists.
            {
                string path = Path.GetDirectoryName(file); // Create config directory if doesn't exist.
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var config = new Configuration(); // Create a new configuration object.

                string token;
                Console.WriteLine("Please enter your productive token: ");
                token = Console.ReadLine(); // Read the productive bot token from console.
                config.ProductiveToken = token;
                Console.WriteLine("Please enter your testing token: ");
                token = Console.ReadLine(); // Read the testing bot token from console.
                config.TestingToken = token;

                config.SaveJson(); // Save the new configuration object to file.
            }
            Console.WriteLine("Configuration Loaded");
        }

        /// <summary> Save the configuration to the path specified in FileName. </summary>
        public void SaveJson()
        {
            string file = Path.Combine(AppContext.BaseDirectory, FileName);
            File.WriteAllText(file, ToJson());
        }

        /// <summary> Load the configuration from the path specified in FileName. </summary>
        public static Configuration Load()
        {
            string file = Path.Combine(AppContext.BaseDirectory, FileName);
            return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(file));
        }

        /// <summary> Convert the configuration to a json string. </summary>
        public string ToJson()
            => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}