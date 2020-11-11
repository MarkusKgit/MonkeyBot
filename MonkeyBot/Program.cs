using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Fclp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonkeyBot;
using MonkeyBot.Common;
using MonkeyBot.Models;
using MonkeyBot.Modules;
using MonkeyBot.Services;
using System;
using System.Reflection;
using System.Threading.Tasks;

public static class Program
{
    private static IServiceProvider services;
    private static DiscordClient discordClient;
    private static CommandsNextExtension commands;

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

        DiscordClientConfiguration cfgJson =  await DiscordClientConfiguration.EnsureExistsAsync().ConfigureAwait(false); // Ensure the configuration file has been created.

        DiscordConfiguration discordConfig = new DiscordConfiguration
        {
            Token = cfgJson.Token,
            TokenType = TokenType.Bot,
            AutoReconnect = true,
            MinimumLogLevel = LogLevel.Debug,
            // TODO: Add NLog here... LoggerFactory = 
        };
        discordClient = new DiscordClient(discordConfig);

        InteractivityConfiguration interactivityConfig = new InteractivityConfiguration
        {
            PaginationBehaviour = PaginationBehaviour.Ignore,
            Timeout = TimeSpan.FromMinutes(5),
            PollBehaviour = PollBehaviour.KeepEmojis,
        };        
        discordClient.UseInteractivity(interactivityConfig);

        services = Initializer.ConfigureServices(discordClient);        
        clientLogger = services.GetRequiredService<ILogger<DiscordClient>>();
        guildService = services.GetRequiredService<IGuildService>();

        CommandsNextConfiguration commandsNextConfig = new CommandsNextConfiguration
        {
            StringPrefixes = new[] { "!" },
            EnableDms = true,
            EnableMentionPrefix = true,
            Services = services
        };
        
        discordClient.Ready += DiscordClient_Ready;
        discordClient.GuildMemberAdded += DiscordClient_GuildMemberAdded;
        discordClient.GuildMemberRemoved += DiscordClient_GuildMemberRemoved;
        discordClient.GuildMemberUpdated += DiscordClient_GuildMemberUpdated;
        discordClient.GuildCreated += DiscordClient_GuildCreated;
        discordClient.GuildDeleted += DiscordClient_GuildDeleted;

        commands = discordClient.UseCommandsNext(commandsNextConfig);
        commands.RegisterCommands(Assembly.GetExecutingAssembly());

        commands.CommandErrored += Commands_CommandErrored;

        await discordClient.ConnectAsync().ConfigureAwait(false);

        await Initializer.InitializeServicesAsync(services).ConfigureAwait(false);

        await Task.Delay(-1).ConfigureAwait(false); // Prevent the console window from closing.
    }

    private static Task DiscordClient_Ready(DiscordClient client, ReadyEventArgs e)
    {
        clientLogger.LogInformation("Client Connected");
        return Task.CompletedTask;
    }

    private static async Task DiscordClient_GuildMemberAdded(DiscordClient client, GuildMemberAddEventArgs e)
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

    private static async Task DiscordClient_GuildMemberRemoved(DiscordClient client, GuildMemberRemoveEventArgs e)
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

    private static async Task DiscordClient_GuildMemberUpdated(DiscordClient client, GuildMemberUpdateEventArgs e)
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

    private static async Task DiscordClient_GuildCreated(DiscordClient client, GuildCreateEventArgs e)
    {
        clientLogger.LogInformation($"Joined guild {e.Guild.Name}");
        // Make sure to create the config;
        _ = await guildService.GetOrCreateConfigAsync(e.Guild.Id).ConfigureAwait(false);
    }

    private static async Task DiscordClient_GuildDeleted(DiscordClient client, GuildDeleteEventArgs e)
    {
        clientLogger.LogInformation($"Left guild {e.Guild.Name}");
        await guildService.RemoveConfigAsync(e.Guild.Id).ConfigureAwait(false);
    }

    private static async Task Commands_CommandErrored(CommandsNextExtension commandsNext, CommandErrorEventArgs e)
    {
        if (e.Exception is OperationCanceledException cex)
        {
            clientLogger.LogWarning(cex, $"Command {e?.Command?.Name ?? ""} was cancelled");
        }
        else
        {
            _ = await e.Context.ErrorAsync($"Command {e?.Command?.Name ?? ""} failed. {e.Exception.Message}").ConfigureAwait(false);
        }
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
        await discordClient.DisconnectAsync().ConfigureAwait(false);
        discordClient.Dispose();
        await Console.Out.WriteLineAsync("Exiting!").ConfigureAwait(false);
    }
}