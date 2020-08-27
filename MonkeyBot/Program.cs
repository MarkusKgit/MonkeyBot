using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Fclp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonkeyBot;
using MonkeyBot.Common;
using MonkeyBot.Models;
using MonkeyBot.Services;
using System;
using System.Reflection;
using System.Threading.Tasks;

public static class Program
{
    private static IServiceProvider services;
    public static DiscordClient DiscordClient { get; private set; }
    public static CommandsNextExtension Commands { get; private set; }

    private static ILogger<DiscordClient> clientLogger;
    private static IGuildService guildService;

    public static async Task Main(string[] args)
    {
        var parser = new FluentCommandLineParser<ApplicationArguments>();
        _ = parser
            .Setup(arg => arg.BuildDocumentation)
            .As('d', "docu")
            .SetDefault(false)
            .WithDescription("Build the documentation files in the app folder");
        _ = parser
            .SetupHelp("?", "help")
            .Callback(text => Console.WriteLine(text));
        ICommandLineParserResult parseResult = parser.Parse(args);
        ApplicationArguments parsedArgs = !parseResult.HasErrors ? parser.Object : null;

        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

        await DiscordClientConfiguration.EnsureExistsAsync().ConfigureAwait(false); // Ensure the configuration file has been created.

        services = await Initializer.InitializeServicesAsync().ConfigureAwait(false);

        clientLogger = services.GetRequiredService<ILogger<DiscordClient>>();
        guildService = services.GetRequiredService<IGuildService>();

        DiscordClientConfiguration cfgJson = await DiscordClientConfiguration.LoadAsync().ConfigureAwait(false);
        DiscordConfiguration discordConfig = new DiscordConfiguration
        {
            Token = cfgJson.Token,
            TokenType = TokenType.Bot,
            AutoReconnect = true,
            LogLevel = DSharpPlus.LogLevel.Debug,
            UseInternalLogHandler = true
        };

        CommandsNextConfiguration commandsNextConfig = new CommandsNextConfiguration
        {
            StringPrefixes = new[] { cfgJson.Token },
            EnableDms = true,
            EnableMentionPrefix = true,
            Services = services
        };

        DiscordClient = new DiscordClient(discordConfig);

        DiscordClient.Ready += DiscordClient_Ready;
        DiscordClient.GuildMemberAdded += DiscordClient_GuildMemberAdded;
        DiscordClient.GuildMemberRemoved += DiscordClient_GuildMemberRemoved;
        DiscordClient.GuildMemberUpdated += DiscordClient_GuildMemberUpdated;
        DiscordClient.GuildCreated += DiscordClient_GuildCreated;
        DiscordClient.GuildDeleted += DiscordClient_GuildDeleted;

        Commands = DiscordClient.UseCommandsNext(commandsNextConfig);
        Commands.RegisterCommands(Assembly.GetExecutingAssembly());

        await DiscordClient.ConnectAsync().ConfigureAwait(false);


        await Task.Delay(-1).ConfigureAwait(false); // Prevent the console window from closing.
    }

    private static Task DiscordClient_Ready(ReadyEventArgs e)
    {
        clientLogger.LogInformation("Client Connected");
        return Task.CompletedTask;
    }

    private static async Task DiscordClient_GuildMemberAdded(GuildMemberAddEventArgs e)
    {
        GuildConfig config = await guildService.GetOrCreateConfigAsync(e.Guild.Id).ConfigureAwait(false);
        string welcomeMessage = config?.WelcomeMessageText ?? string.Empty;
        if (config?.WelcomeMessageChannelId != null && !welcomeMessage.IsEmpty())
        {
            DiscordChannel channel = e.Guild.GetChannel(config.WelcomeMessageChannelId) ?? e.Guild.GetDefaultChannel();
            welcomeMessage = welcomeMessage.Replace("%server%", e.Guild.Name).Replace("%user%", e.Member.Username);
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.DarkBlue)
                .WithDescription(welcomeMessage)
                .WithThumbnail(new Uri(e.Member.AvatarUrl ?? e.Member.DefaultAvatarUrl))
                .WithTimestamp(DateTime.Now);

            _ = await (channel?.SendMessageAsync(embed: builder.Build())).ConfigureAwait(false);
        }
    }

    private static async Task DiscordClient_GuildMemberRemoved(GuildMemberRemoveEventArgs e)
    {
        GuildConfig config = await guildService.GetOrCreateConfigAsync(e.Guild.Id).ConfigureAwait(false);
        string goodbyeMessage = config?.GoodbyeMessageText ?? string.Empty;
        if (config?.GoodbyeMessageChannelId != null && !goodbyeMessage.IsEmpty())
        {
            DiscordChannel channel = e.Guild.GetChannel(config.WelcomeMessageChannelId) ?? e.Guild.GetDefaultChannel();
            goodbyeMessage = goodbyeMessage.Replace("%server%", e.Guild.Name).Replace("%user%", e.Member.Username);
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.DarkBlue)
                .WithDescription(goodbyeMessage)
                .WithThumbnail(new Uri(e.Member.AvatarUrl ?? e.Member.DefaultAvatarUrl))
                .WithTimestamp(DateTime.Now);

            _ = await (channel?.SendMessageAsync(embed: builder.Build())).ConfigureAwait(false);
        }
    }

    private static async Task DiscordClient_GuildMemberUpdated(GuildMemberUpdateEventArgs e)
    {
        GuildConfig config = await guildService.GetOrCreateConfigAsync(e.Guild.Id).ConfigureAwait(false);
        if (config == null || !config.StreamAnnouncementsEnabled || config.ConfirmedStreamerIds == null || !config.ConfirmedStreamerIds.Contains(e.Member.Id))
        {
            // Streaming announcements has to be enabled for the guild and the streamer must first opt in to have it announced
            return;
        }

        if (e.Member.Presence.Activity.ActivityType == ActivityType.Streaming)
        {
            DiscordChannel channel = e.Guild.GetDefaultChannel();
            if (config?.DefaultChannelId != null)
            {
                channel = e.Guild.GetChannel(config.WelcomeMessageChannelId) ?? channel;
            }
            _ = await (channel?.SendMessageAsync($"{e.Member.Username} has started streaming. Watch it [here]({e.Member.Presence.Activity.StreamUrl}) ")).ConfigureAwait(false);
        }
    }

    private static async Task DiscordClient_GuildCreated(GuildCreateEventArgs e)
    {
        clientLogger.LogInformation($"Joined guild {e.Guild.Name}");
        // Make sure to create the config;
        _ = await guildService.GetOrCreateConfigAsync(e.Guild.Id).ConfigureAwait(false);
    }

    private static async Task DiscordClient_GuildDeleted(GuildDeleteEventArgs e)
    {
        clientLogger.LogInformation($"Left guild {e.Guild.Name}");
        await guildService.RemoveConfigAsync(e.Guild.Id).ConfigureAwait(false);
    }

    private static async void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            await Console.Out.WriteLineAsync($"Unhandled exception: {ex.Message}").ConfigureAwait(false);
        }

        if (e.IsTerminating)
        {
            await Console.Out.WriteLineAsync("Terminating!").ConfigureAwait(false);
        }
    }

    private static async void CurrentDomain_ProcessExit(object sender, EventArgs e)
    {
        await DiscordClient.DisconnectAsync().ConfigureAwait(false);
        DiscordClient.Dispose();
        await Console.Out.WriteLineAsync("Exiting!").ConfigureAwait(false);
    }
}