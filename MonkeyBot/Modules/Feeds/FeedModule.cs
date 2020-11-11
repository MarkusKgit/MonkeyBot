using CodeHollow.FeedReader;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MonkeyBot.Common;
using MonkeyBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [Description("Feeds")]
    [MinPermissions(AccessLevel.ServerAdmin)]
    [RequireGuild]
    [RequireBotPermissions(Permissions.EmbedLinks)]
    public class FeedModule : BaseCommandModule
    {
        private readonly IFeedService feedService;

        public FeedModule(IFeedService feedService)
        {
            this.feedService = feedService;
        }

        [Command("AddFeed")]
        [Description("Adds an atom or RSS feed to the list of listened feeds.")]
        [Example("!Feeds add https://blogs.msdn.microsoft.com/dotnet/feed/")]
        public async Task AddFeedUrlAsync(CommandContext ctx, [Description("The name/title of the feed")] string name, [Description("The url to the feed (Atom/RSS)")] string url, [Description("Optional: The channel where the Feed updates should be posted. Defaults to current channel")] DiscordChannel channel = null)
        {
            if (name.IsEmpty())
            {
                _ = await ctx.ErrorAsync("Please enter a name for the feed!").ConfigureAwait(false);
                return;
            }
            if (url.IsEmpty())
            {
                _ = await ctx.ErrorAsync("Please enter a feed url!").ConfigureAwait(false);
                return;
            }
            channel ??= ctx.Channel;

            IEnumerable<HtmlFeedLink> urls = await FeedReader.GetFeedUrlsFromUrlAsync(url).ConfigureAwait(false);
            string feedUrl;
            if (!urls.Any()) // no url - probably the url is already the right feed url
            {
                feedUrl = url;
            }
            else if (urls.Count() == 1)
            {
                feedUrl = urls.First().Url;
            }
            else
            {
                _ = await ctx.ErrorAsync($"Multiple feeds were found at this url. Please be more specific:{Environment.NewLine}{string.Join(Environment.NewLine, urls)}").ConfigureAwait(false);
                return;
            }
            List<GuildFeed> currentFeeds = await feedService.GetFeedsForGuildAsync(ctx.Guild.Id, channel.Id).ConfigureAwait(false);
            if (currentFeeds.Any(x => x.Url == feedUrl || x.Name == name))
            {
                _ = await ctx.ErrorAsync("The specified feed is already in the list!").ConfigureAwait(false);
                return;
            }
            await feedService.AddFeedAsync(name, feedUrl, ctx.Guild.Id, channel.Id).ConfigureAwait(false);
            _ = await ctx.OkAsync("Feed added").ConfigureAwait(false);
        }

        [Command("RemoveFeed")]
        [Description("Removes the specified feed from the list of feeds.")]
        [Example("!Feeds remove https://blogs.msdn.microsoft.com/dotnet/feed/")]
        public async Task RemoveFeedUrlAsync(CommandContext ctx, [Description("The name or the url of the feed")] string nameOrUrl)
        {
            if (nameOrUrl.IsEmpty())
            {
                _ = await ctx.ErrorAsync("Please enter the feed's name or url").ConfigureAwait(false);
                return;
            }
            List<GuildFeed> currentFeeds = await feedService.GetFeedsForGuildAsync(ctx.Guild.Id).ConfigureAwait(false);
            if (!currentFeeds.Any(x => x.Url == nameOrUrl || x.Name == nameOrUrl))
            {
                _ = await ctx.ErrorAsync("The specified feed is not in the list!").ConfigureAwait(false);
                return;
            }

            await feedService.RemoveFeedAsync(nameOrUrl, ctx.Guild.Id).ConfigureAwait(false);
            _ = await ctx.OkAsync("Feed removed").ConfigureAwait(false);
        }

        [Command("ListFeeds")]
        [Description("List all current feed urls")]
        public async Task ListFeedUrlsAsync(CommandContext ctx, [Description("Optional: The channel where the Feed urls should be listed for. Defaults to all channels")] DiscordChannel channel = null)
        {
            List<GuildFeed> guildFeeds = await feedService.GetFeedsForGuildAsync(ctx.Guild.Id, channel?.Id).ConfigureAwait(false);
            if (guildFeeds == null || guildFeeds.Count < 1)
            {
                _ = await ctx.ErrorAsync("No feeds have been added yet.").ConfigureAwait(false);
            }
            else
            {
                if (channel == null)
                {
                    var sb = new StringBuilder();
                    foreach (GuildFeed guildFeed in guildFeeds)
                    {
                        DiscordChannel feedChannel = ctx.Guild.GetChannel(guildFeed.ChannelId);
                        _ = sb.AppendLine($"{feedChannel.Mention}: {guildFeed.Name} ({guildFeed.Url})");
                    }
                    _ = await ctx.RespondAsync($"The following feeds are listed in all channels:{Environment.NewLine}{sb}").ConfigureAwait(false);
                }
                else
                {
                    string allUrls = string.Join(Environment.NewLine, guildFeeds.Select(x => x.Url));
                    _ = await ctx.RespondAsync($"The following feeds are listed in {channel.Mention}:{Environment.NewLine}{string.Join(Environment.NewLine, guildFeeds.Select(x => x.Url))}").ConfigureAwait(false);
                }
            }
        }

        [Command("RemoveAllFeeds")]
        [Description("Removes all feed urls")]
        public async Task RemoveFeedUrlsAsync(CommandContext ctx, [Description("Optional: The channel where the Feed urls should be removed. Defaults to all channels")] DiscordChannel channel = null)
        {
            await feedService.RemoveAllFeedsAsync(ctx.Guild.Id, channel?.Id).ConfigureAwait(false);
            _ = await ctx.OkAsync(channel == null ? "All Feeds removed" : $"All Feeds in {channel.Mention} removed").ConfigureAwait(false);
        }
    }
}