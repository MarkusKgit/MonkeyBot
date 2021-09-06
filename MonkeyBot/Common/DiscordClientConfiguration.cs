using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MonkeyBot.Common
{
    /// <summary>
    /// Contains configuration settings including token which should not be public
    /// </summary>
    public class DiscordClientConfiguration
    {
        /// <summary> The location and name of the bot's configuration file. </summary>
        private static readonly string configFilePath = Path.Combine(AppContext.BaseDirectory, "config", "configuration.json");

        /// <summary> Ids of users who will have owner access to the bot. </summary>
        private List<ulong> owners = new();

        [JsonPropertyName("Owners")]
        public IReadOnlyList<ulong> Owners
        {
            get => owners;
            init => owners = new List<ulong>(value);
        }

        /// <summary> The bot's login token. </summary>
        [JsonPropertyName("Token")]
        public string Token { get; set; }

        /// <summary>Makes sure that a config file exists and asks for the token on first run</summary>
        public static async Task<DiscordClientConfiguration> EnsureExistsAsync()
        {
            if (!File.Exists(configFilePath))
            {
                string path = Path.GetDirectoryName(configFilePath);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var config = new DiscordClientConfiguration();

                // Get the token
                await Console.Out.WriteLineAsync("Please enter the bot's access token: ");
                config.Token = await Console.In.ReadLineAsync();

                // Get owner
                await Console.Out.WriteLineAsync("Please enter the Discord Id of the Bot owner (leave blank for default): ");
                string sOwnerId = await Console.In.ReadLineAsync();
                if (ulong.TryParse(sOwnerId, out ulong ownerId) && ownerId > 0)
                {
                    config.AddOwner(ownerId);
                }
                else
                {
                    config.AddOwner(327885109560737793);
                }
                
                await config.SaveAsync(); // Save the new configuration object to file.
                return config;
            }
            return await LoadAsync();
        }

        public void AddOwner(ulong ownerId)
        {
            if (!owners.Contains(ownerId))
            {
                owners.Add(ownerId);
            }
        }

        public void RemoveOwner(ulong ownerId)
        {
            if (owners.Contains(ownerId))
            {
                owners.Remove(ownerId);
            }
        }

        /// <summary> Save the configuration to the path specified in FileName. </summary>
        public Task SaveAsync() 
            => MonkeyHelpers.WriteTextAsync(configFilePath, ToJson());

        /// <summary> Load the configuration from the path specified in FileName. </summary>
        public static async Task<DiscordClientConfiguration> LoadAsync()
        {
            string json = await MonkeyHelpers.ReadTextAsync(configFilePath);
            DiscordClientConfiguration config = JsonSerializer.Deserialize<DiscordClientConfiguration>(json);
            return config;
        }

        /// <summary> Convert the configuration to a json string. </summary>
        private string ToJson()
            => JsonSerializer.Serialize(this, new JsonSerializerOptions() {WriteIndented = true});
    }
}