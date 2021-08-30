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
using MonkeyBot.Common;
using MonkeyBot.Models;
using MonkeyBot.Services;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace MonkeyBot
{
    public static class Program
    {
        private static IServiceProvider _services;
        private static DiscordClient _discordClient;
        private static CommandsNextExtension _commandsNext;
        private static IGuildService _guildService;

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

            DiscordClientConfiguration cfgJson = await DiscordClientConfiguration.EnsureExistsAsync(); // Ensure the configuration file has been created.
            var loggingConfig = SetupNLogConfig();

            var discordConfig = new DiscordConfiguration
            {
                Token = cfgJson.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Information,
                Intents = DiscordIntents.All,
                LoggerFactory = LoggerFactory.Create(builder => builder.AddNLog(loggingConfig))
            };
            _discordClient = new DiscordClient(discordConfig);

            var interactivityConfig = new InteractivityConfiguration
            {
                PaginationBehaviour = PaginationBehaviour.Ignore,
                Timeout = TimeSpan.FromMinutes(5),
                PollBehaviour = PollBehaviour.KeepEmojis,
            };
            _discordClient.UseInteractivity(interactivityConfig);

            _services = Initializer.ConfigureServices(_discordClient, loggingConfig);
            _guildService = _services.GetRequiredService<IGuildService>();

            var commandsNextConfig = new CommandsNextConfiguration
            {
                StringPrefixes = new[] { "!" },
                EnableDms = true,
                EnableMentionPrefix = true,
                Services = _services
            };

            _discordClient.Ready += DiscordClient_Ready;
            _discordClient.GuildMemberAdded += DiscordClient_GuildMemberAdded;
            _discordClient.GuildMemberRemoved += DiscordClient_GuildMemberRemoved;
            _discordClient.GuildMemberUpdated += DiscordClient_GuildMemberUpdated;
            _discordClient.GuildCreated += DiscordClient_GuildCreated;
            _discordClient.GuildDeleted += DiscordClient_GuildDeleted;

            _commandsNext = _discordClient.UseCommandsNext(commandsNextConfig);
            _commandsNext.RegisterCommands(Assembly.GetExecutingAssembly());

            _commandsNext.CommandErrored += Commands_CommandErrored;

            await _discordClient.ConnectAsync();

            await Initializer.InitializeServicesAsync(_services);

            await Task.Delay(-1); // Prevent the console window from closing.
        }

        private static LoggingConfiguration SetupNLogConfig()
        {
            var logConfig = new LoggingConfiguration();

            var coloredConsoleTarget = new ColoredConsoleTarget("logconsole")
            {
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} ${logger:shortName=True} | ${message} ${exception:format=ToString,Data:exceptionDataSeparator=\r\n}"
            };

            logConfig.AddTarget(coloredConsoleTarget);

            var debugTarget = new DebuggerTarget
            {
                Name = "debugConsole",
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} ${logger:shortName=True} | ${message} ${exception:format=ToString,Data:exceptionDataSeparator=\r\n}"
            };
            logConfig.AddTarget(debugTarget);

            var fileTarget = new FileTarget
            {
                Name = "logFile",
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} ${logger:shortName=True} | ${message} ${exception:format=ToString,Data:exceptionDataSeparator=\r\n}",
                FileName = "${basedir}/Logs/${level}.log",
                ArchiveFileName = "${basedir}/Logs/Archive/${level}.{##}.log",
                ArchiveNumbering = ArchiveNumberingMode.Sequence,
                ArchiveAboveSize = 1_000_000,
                ConcurrentWrites = false,
                MaxArchiveFiles = 20
            };
            logConfig.AddTarget(fileTarget);

            logConfig.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, coloredConsoleTarget);
            logConfig.AddRuleForAllLevels(debugTarget);
            logConfig.AddRuleForAllLevels(fileTarget);

            return logConfig;
        }

        private static Task DiscordClient_Ready(DiscordClient client, ReadyEventArgs e)
        {   
            _discordClient.Logger.LogInformation("Client Connected");
            return Task.CompletedTask;
        }

        private static async Task DiscordClient_GuildMemberAdded(DiscordClient client, GuildMemberAddEventArgs e)
        {
            GuildConfig config = await _guildService.GetOrCreateConfigAsync(e.Guild.Id);
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

                _ = await (channel?.SendMessageAsync(embed: builder.Build()));
            }
        }

        private static async Task DiscordClient_GuildMemberRemoved(DiscordClient client, GuildMemberRemoveEventArgs e)
        {
            GuildConfig config = await _guildService.GetOrCreateConfigAsync(e.Guild.Id);
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

                _ = await (channel?.SendMessageAsync(embed: builder.Build()));
            }
        }

        private static async Task DiscordClient_GuildMemberUpdated(DiscordClient client, GuildMemberUpdateEventArgs e)
        {
            GuildConfig config = await _guildService.GetOrCreateConfigAsync(e.Guild.Id);
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
                _ = await (channel?.SendMessageAsync($"{e.Member.Username} has started streaming. Watch it [here]({e.Member.Presence.Activity.StreamUrl}) "));
            }
        }

        private static async Task DiscordClient_GuildCreated(DiscordClient client, GuildCreateEventArgs e)
        {
            _discordClient.Logger.LogInformation($"Joined guild {e.Guild.Name}");
            // Make sure to create the config;
            _ = await _guildService.GetOrCreateConfigAsync(e.Guild.Id);
        }

        private static async Task DiscordClient_GuildDeleted(DiscordClient client, GuildDeleteEventArgs e)
        {
            _discordClient.Logger.LogInformation($"Left guild {e.Guild.Name}");
            await _guildService.RemoveConfigAsync(e.Guild.Id);
        }

        private static async Task Commands_CommandErrored(CommandsNextExtension commandsNext, CommandErrorEventArgs e)
        {
            if (e.Exception is OperationCanceledException cex)
            {
                _discordClient.Logger.LogWarning(cex, $"Command {e?.Command?.Name ?? ""} was cancelled");
            }
            else
            {
                //TODO: Handle the actual errors more fine grained
                await e.Context.ErrorAsync($"Command {e?.Command?.Name ?? ""} failed. {e.Exception.Message}");
            }
        }

        private static async void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                await Console.Out.WriteLineAsync($"Unhandled exception: {ex.Message}");
            }

            if (e.IsTerminating)
            {
                await Console.Out.WriteLineAsync("Terminating!");
            }
        }

        private static async void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            if (_discordClient != null)
            {
                await _discordClient.DisconnectAsync();
                _discordClient.Dispose();
            }
            await Console.Out.WriteLineAsync("Exiting!");
        }
    }
}