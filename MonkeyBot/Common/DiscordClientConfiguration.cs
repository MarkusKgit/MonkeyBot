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
        private const string fileName = "config/configuration.json";

        /// <summary> Ids of users who will have owner access to the bot. </summary>
        private List<ulong> owners = new List<ulong>();

        // TODO: Make setter private once System.Text.Json supports it
        [JsonPropertyName("Owners")]
        public IReadOnlyList<ulong> Owners
        {
            get => owners;
            set => owners = new List<ulong>(value);
        }

        /// <summary> The bot's login token. </summary>
        [JsonPropertyName("Token")]
        public string Token { get; set; }

        /// <summary> Api credentials for cloudinary for uploading pictures. </summary>
        [JsonPropertyName("CloudinaryCredentials")]
        public CloudinaryCredentials CloudinaryCredentials { get; set; }

        /// <summary>Makes sure that a config file exists and asks for the token on first run</summary>
        public static async Task EnsureExistsAsync()
        {
            string file = Path.Combine(AppContext.BaseDirectory, fileName);
            if (!File.Exists(file))
            {
                string path = Path.GetDirectoryName(file);
                if (!Directory.Exists(path))
                {
                    _ = Directory.CreateDirectory(path);
                }

                var config = new DiscordClientConfiguration();

                // Get the token
                await Console.Out.WriteLineAsync("Please enter the bot's access token: ").ConfigureAwait(false);
                config.Token = await Console.In.ReadLineAsync().ConfigureAwait(false);

                // Get owner
                await Console.Out.WriteLineAsync("Please enter the Discord Id of the Bot owner (leave blank for default): ").ConfigureAwait(false);
                string sOwnerId = await Console.In.ReadLineAsync().ConfigureAwait(false);
                if (ulong.TryParse(sOwnerId, out ulong ownerId) && ownerId > 0)
                {
                    config.AddOwner(ownerId);
                }
                else
                {
                    config.AddOwner(327885109560737793);
                }

                // Get cloudinary credentials
                await Console.Out.WriteLineAsync("Do you want to setup cloudinary? (y/n)").ConfigureAwait(false);
                string ans = await Console.In.ReadLineAsync().ConfigureAwait(false);
                if (ans.StartsWith("y", StringComparison.OrdinalIgnoreCase))
                {
                    var creds = new CloudinaryCredentials();
                    await Console.Out.WriteLineAsync("Enter your cloud id").ConfigureAwait(false);
                    ans = await Console.In.ReadLineAsync().ConfigureAwait(false);
                    creds.Cloud = ans;
                    await Console.Out.WriteLineAsync("Enter your Api Key").ConfigureAwait(false);
                    ans = await Console.In.ReadLineAsync().ConfigureAwait(false);
                    creds.ApiKey = ans;
                    await Console.Out.WriteLineAsync("Enter your Api Secret").ConfigureAwait(false);
                    ans = await Console.In.ReadLineAsync().ConfigureAwait(false);
                    creds.ApiSecret = ans;
                    config.CloudinaryCredentials = creds;
                }
                
                await config.SaveAsync().ConfigureAwait(false); // Save the new configuration object to file.
            }
            await Console.Out.WriteLineAsync("Configuration Loaded").ConfigureAwait(false);
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
                _ = owners.Remove(ownerId);
            }
        }

        /// <summary> Save the configuration to the path specified in FileName. </summary>
        public Task SaveAsync()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, fileName);
            return MonkeyHelpers.WriteTextAsync(filePath, ToJson());
        }

        /// <summary> Load the configuration from the path specified in FileName. </summary>
        public static async Task<DiscordClientConfiguration> LoadAsync()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, fileName);
            string json = await MonkeyHelpers.ReadTextAsync(filePath).ConfigureAwait(false);
            DiscordClientConfiguration config = JsonSerializer.Deserialize<DiscordClientConfiguration>(json);
            return config;
        }

        /// <summary> Convert the configuration to a json string. </summary>
        private string ToJson()
            => JsonSerializer.Serialize(this, new JsonSerializerOptions() {WriteIndented = true});
    }
}