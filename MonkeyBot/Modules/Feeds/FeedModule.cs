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
    public class FeedModule : BaseCommandModule
    {
        private readonly IFeedService _feedService;

        public FeedModule(IFeedService feedService)
        {
            _feedService = feedService;
        }

        [Command("Feeds")]
        [Description("View all options to manage feeds")]
        public async Task FeedsAsync(CommandContext ctx)
        {
            var addButton = new DiscordButtonWithCallback(ctx.Client, ButtonStyle.Primary, "AddFeed".WithGuid(), "Add Feed", null, () => new DiscordInteractionResponseBuilder().WithContent("You want to add a feed"));
            var removeButton = new DiscordButtonWithCallback(ctx.Client, ButtonStyle.Primary, "RemoveFeed".WithGuid(), "Remove Feed", null, () => new DiscordFollowupMessageBuilder().WithContent("You want to remove a feed"));
            var listButton = new DiscordButtonWithCallback(ctx.Client, ButtonStyle.Primary, "ListFeeds".WithGuid(), "List Feeds", null, HandleListFeeds);
            var msgBuilder = new DiscordMessageBuilder()
                .WithContent("What do you want to do?")
                .AddComponents(addButton, removeButton, listButton);
            var msg = await ctx.RespondAsync(msgBuilder);
        }

        private DiscordFollowupMessageBuilder HandleListFeeds()
        {
            return new DiscordFollowupMessageBuilder()
                       .WithContent("Oh you want the feeds listed?");
        }

        [Command("AddFeed")]
        [Description("Adds an atom or RSS feed to the list of listened feeds.")]
        [Example("AddFeed DotNet https://blogs.msdn.microsoft.com/dotnet/feed/")]
        [Example("AddFeed DotNet https://blogs.msdn.microsoft.com/dotnet/feed/ #news")]
        public async Task AddFeedUrlAsync(CommandContext ctx, [Description("The name/title of the feed")] string name, [Description("The url to the feed (Atom/RSS)")] string url, [Description("Optional: The channel where the Feed updates should be posted. Defaults to current channel")] DiscordChannel channel = null)
        {
            if (name.IsEmpty())
            {
                _ = await ctx.ErrorAsync("Please enter a name for the feed!");
                return;
            }
            if (url.IsEmpty())
            {
                _ = await ctx.ErrorAsync("Please enter a feed url!");
                return;
            }
            channel ??= ctx.Channel;

            IEnumerable<HtmlFeedLink> urls = await FeedReader.GetFeedUrlsFromUrlAsync(url);
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
                _ = await ctx.ErrorAsync($"Multiple feeds were found at this url. Please be more specific:{Environment.NewLine}{string.Join(Environment.NewLine, urls)}");
                return;
            }
            List<GuildFeed> currentFeeds = await _feedService.GetFeedsForGuildAsync(ctx.Guild.Id, channel.Id);
            if (currentFeeds.Any(x => x.Url == feedUrl || x.Name == name))
            {
                _ = await ctx.ErrorAsync("The specified feed is already in the list!");
                return;
            }
            await _feedService.AddFeedAsync(name, feedUrl, ctx.Guild.Id, channel.Id);
            _ = await ctx.OkAsync("Feed added");
        }

        [Command("RemoveFeed")]
        [Description("Removes the specified feed from the list of feeds.")]
        [Example("RemoveFeed DotNet")]
        [Example("RemoveFeed https://blogs.msdn.microsoft.com/dotnet/feed/")]
        public async Task RemoveFeedUrlAsync(CommandContext ctx, [Description("The name or the url of the feed")] string nameOrUrl)
        {
            if (nameOrUrl.IsEmpty())
            {
                _ = await ctx.ErrorAsync("Please enter the feed's name or url");
                return;
            }
            List<GuildFeed> currentFeeds = await _feedService.GetFeedsForGuildAsync(ctx.Guild.Id);
            if (!currentFeeds.Any(x => x.Url == nameOrUrl || x.Name == nameOrUrl))
            {
                _ = await ctx.ErrorAsync("The specified feed is not in the list!");
                return;
            }

            await _feedService.RemoveFeedAsync(nameOrUrl, ctx.Guild.Id);
            _ = await ctx.OkAsync("Feed removed");
        }

        [Command("ListFeeds")]
        [Description("List all current feed urls")]
        [Example("ListFeeds #news")]
        public async Task ListFeedUrlsAsync(CommandContext ctx, [Description("Optional: The channel where the Feed urls should be listed for. Defaults to all channels")] DiscordChannel channel = null)
        {
            List<GuildFeed> guildFeeds = await _feedService.GetFeedsForGuildAsync(ctx.Guild.Id, channel?.Id);
            if (guildFeeds == null || guildFeeds.Count < 1)
            {
                _ = await ctx.ErrorAsync("No feeds have been added yet.");
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
                    _ = await ctx.RespondAsync($"The following feeds are listed in all channels:{Environment.NewLine}{sb}");
                }
                else
                {
                    string allUrls = string.Join(Environment.NewLine, guildFeeds.Select(x => x.Url));
                    _ = await ctx.RespondAsync($"The following feeds are listed in {channel.Mention}:{Environment.NewLine}{string.Join(Environment.NewLine, guildFeeds.Select(x => x.Url))}");
                }
            }
        }

        [Command("RemoveAllFeeds")]
        [Description("Removes all feed urls")]
        [Example("RemoveAllFeeds #news")]
        public async Task RemoveFeedUrlsAsync(CommandContext ctx, [Description("Optional: The channel where the Feed urls should be removed. Defaults to all channels")] DiscordChannel channel = null)
        {
            await _feedService.RemoveAllFeedsAsync(ctx.Guild.Id, channel?.Id);
            _ = await ctx.OkAsync(channel == null ? "All Feeds removed" : $"All Feeds in {channel.Mention} removed");
        }
    }
}