using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IFeedService
    {
        /// <summary>
        /// Start the feed service to regularly check for updates
        /// </summary>
        void Start();

        /// <summary>
        /// Add a new feed url. Updates will be posted in the provided channel
        /// </summary>
        /// <param name="name">Identifier/Title for the feed</param>
        /// <param name="url">URL of the feed (atom/rss)</param>
        /// <param name="guildId"></param>
        /// <param name="channelId"></param>
        /// <returns></returns>
        Task AddFeedAsync(string name, string url, ulong guildId, ulong channelId);

        /// <summary>
        /// Removes the specified feed url from the channel
        /// </summary>
        /// <param name="nameOrUrl">Name or URL of the feed</param>
        /// <param name="guildId"></param>
        /// <param name="channelId"></param>
        /// <returns></returns>
        Task RemoveFeedAsync(string nameOrUrl, ulong guildId);

        /// <summary>
        /// Removes all feeds. If a channel is provided only the feeds in this channel are removed
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="channelId"></param>
        /// <returns></returns>
        Task RemoveAllFeedsAsync(ulong guildId, ulong? channelId);

        /// <summary>
        /// Returns all subscribed feeds. If a channel is provided only feeds in this channel are returned.
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="channelId"></param>
        /// <returns></returns>
        Task<List<GuildFeed>> GetFeedsForGuildAsync(ulong guildId, ulong? channelId = null);

        /// <summary>
        /// Get a list of possible feeds urls at a base url
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <returns></returns>
        Task<IEnumerable<string>> GetFeedUrls(string baseUrl);
    }
}