using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using MonkeyBot.Common;
using MonkeyBot.Database;
using MonkeyBot.Models;
using MonkeyBot.Services;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MonkeyBot
{
    public static class Initializer
    {
        private static DiscordClient _discordClient;
        private static IServiceProvider _services;
        private static IGuildService _guildService;
        private static InteractivityExtension _interactivity;
        private static CommandsNextExtension _commandsNext;

        public static async Task<IServiceProvider> InitializeServicesAndStartClientAsync()
        {
            var loggingConfig = SetupNLogConfig();
            _discordClient = await SetupDiscordClient(loggingConfig);
            _services = ConfigureServices(_discordClient, loggingConfig);
            _guildService = _services.GetRequiredService<IGuildService>();
            _interactivity = SetupInteractivity();
            _commandsNext = SetupCommandsNext();
            SetupEventHandlers();
            await _discordClient.ConnectAsync();
            await StartServicesAsync(_services);
            return _services;
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
        private static async Task<DiscordClient> SetupDiscordClient(LoggingConfiguration loggingConfig)
        {
            var cfgJson = await DiscordClientConfiguration.EnsureExistsAsync(); // Ensure the configuration file has been created.

            var discordConfig = new DiscordConfiguration
            {
                Token = cfgJson.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Information,
                Intents = DiscordIntents.All,
                LoggerFactory = LoggerFactory.Create(builder => builder.AddNLog(loggingConfig))
            };
            var discordClient = new DiscordClient(discordConfig);

            return discordClient;
        }

        private static IServiceProvider ConfigureServices(DiscordClient discordClient, LoggingConfiguration loggingConfiguration)
        {
            IServiceCollection services = new ServiceCollection()
                .AddLogging(loggingBuilder =>
                    {
                        _ = loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                        _ = loggingBuilder.AddNLog(loggingConfiguration);
                    })
                .AddHttpClient()
                .AddDbContext<MonkeyDBContext>(ServiceLifetime.Transient)
                .AddSingleton(discordClient)
                .AddSingleton<IGuildService, GuildService>()
                .AddSingleton<ISchedulingService, SchedulingService>()
                .AddSingleton<IAnnouncementService, AnnouncementService>()
                .AddSingleton<IFeedService, FeedService>()
                .AddSingleton<IBattlefieldNewsService, BattlefieldNewsService>()
                .AddSingleton<SteamGameServerService>()
                .AddSingleton<MineCraftGameServerService>()
                .AddSingleton<IRoleButtonService, RoleButtonService>()
                .AddSingleton<IChuckService, ChuckService>()
                .AddSingleton<ICatService, CatService>()
                .AddSingleton<IDogService, DogService>()
                .AddSingleton<IXkcdService, XkcdService>()
                .AddSingleton<IPictureSearchService, GoogleImageSearchService>()
                .AddSingleton<ITriviaService, TriviaService>()
                .AddSingleton<IPollService, PollService>();

            //Remove unnecessary Http Client log clutter
            services.RemoveAll<IHttpMessageHandlerBuilderFilter>();

            IServiceProvider provider = new DefaultServiceProviderFactory().CreateServiceProvider(services);

            return provider;
        }

        private static async Task StartServicesAsync(IServiceProvider services)
        {
            MonkeyDBContext dbContext = services.GetRequiredService<MonkeyDBContext>();
            await DBInitializer.InitializeAsync(dbContext);

            IAnnouncementService announcements = services.GetService<IAnnouncementService>();
            await announcements.InitializeAsync();

            SteamGameServerService steamGameServerService = services.GetService<SteamGameServerService>();
            steamGameServerService.Initialize();

            MineCraftGameServerService minecraftGameServerService = services.GetService<MineCraftGameServerService>();
            minecraftGameServerService.Initialize();

            IRoleButtonService roleButtonsService = services.GetService<IRoleButtonService>();
            roleButtonsService.Initialize();

            IFeedService feedService = services.GetService<IFeedService>();
            feedService.Start();

            IBattlefieldNewsService battlefieldNewsService = services.GetService<IBattlefieldNewsService>();
            battlefieldNewsService.Start();

            IPollService pollService = services.GetService<IPollService>();
            await pollService.InitializeAsync();

            return;
        }

        private static InteractivityExtension SetupInteractivity()
        {
            var interactivityConfig = new InteractivityConfiguration
            {
                PaginationBehaviour = PaginationBehaviour.Ignore,
                Timeout = TimeSpan.FromMinutes(5),
                PollBehaviour = PollBehaviour.KeepEmojis
            };
            var interactivity = _discordClient.UseInteractivity(interactivityConfig);
            return interactivity;
        }

        private static CommandsNextExtension SetupCommandsNext()
        {
            var commandsNextConfig = new CommandsNextConfiguration
            {
                EnableMentionPrefix = true,
                PrefixResolver = HandlePrefixAsync,
                EnableDms = true,
                IgnoreExtraArguments = true,                
                Services = _services
            };
            var commandsNext = _discordClient.UseCommandsNext(commandsNextConfig);
            commandsNext.RegisterCommands(Assembly.GetExecutingAssembly());
            commandsNext.CommandErrored += Commands_CommandErrored;

            return commandsNext;
        }

        private static async Task<int> HandlePrefixAsync(DiscordMessage msg)
        {
            if (msg.Channel.Guild != null)
            {
                string prefix = (await _guildService.GetOrCreateConfigAsync(msg.Channel.Guild.Id)).CommandPrefix;
                return msg.GetStringPrefixLength(prefix);
            }
            return msg.GetStringPrefixLength("!"); // Default prefix;
        }

        private static void SetupEventHandlers()
        {
            _discordClient.Ready += DiscordClient_Ready;
            _discordClient.GuildDownloadCompleted += DiscordClient_GuildDownloadCompleted;
            _discordClient.GuildMemberAdded += DiscordClient_GuildMemberAdded;
            _discordClient.GuildMemberRemoved += DiscordClient_GuildMemberRemoved;
            _discordClient.GuildMemberUpdated += DiscordClient_GuildMemberUpdated;
            _discordClient.GuildCreated += DiscordClient_GuildCreated;
            _discordClient.GuildDeleted += DiscordClient_GuildDeleted;
        }

        private static Task DiscordClient_Ready(DiscordClient client, ReadyEventArgs e)
        {
            _discordClient.Logger.LogInformation("Client Connected");
            return Task.CompletedTask;
        }

        private static Task DiscordClient_GuildDownloadCompleted(DiscordClient sender, GuildDownloadCompletedEventArgs e)
        {
            var guildInfo = e.Guilds.Select(g => $"{g.Value.Name}: {g.Value.MemberCount} members");
            _discordClient.Logger.LogInformation($"Guild Download Complete:\n{string.Join("\n", guildInfo)}");            
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
            string prefix = "!";
            if (e.Context.Guild != null)
            {
                var guildConfig = await _guildService.GetOrCreateConfigAsync(e.Context.Guild.Id);
                prefix = guildConfig.CommandPrefix;
            }
            if (e.Exception is OperationCanceledException cex)
            {
                _discordClient.Logger.LogWarning(cex, $"Command {e?.Command?.Name ?? ""} was cancelled");
            }
            else if (e.Exception is CommandNotFoundException commandNotFound)
            {
                await e.Context.ErrorAsync($"The specified command *{commandNotFound.CommandName}* was not found. Try {prefix}help to get a list of commands");
            }
            else if (e.Exception is ChecksFailedException checksFailed)
            {
                var failedChecks = string.Join("\n", checksFailed.FailedChecks.Select(check => TranslateFailedCheck(check, e.Context)));
                await e.Context.ErrorAsync($"The specified command failed because the following checks failed:\n{failedChecks}");
            }
            else if (e.Exception is ArgumentException arg)
            {
                await e.Context.ErrorAsync($"You provided the wrong arguments for the command. Try {prefix}help {e.Command.Name}");
            }
            else
            {   
                await e.Context.ErrorAsync($"Command {e?.Command?.Name ?? ""} failed:\n{e.Exception.GetType()}\n{e.Exception.Message}");
            }
        }

        private static string TranslateFailedCheck(CheckBaseAttribute check, CommandContext ctx)
        {
            return check switch
            {
                RequireOwnerAttribute => "You must be the bot's owner to use this command",
                RequireGuildAttribute => "The command can only be used in a Guild channel (not in a DM)",
                RequireDirectMessageAttribute => "The command can only be used in a Direct Message",
                RequireBotPermissionsAttribute botperm => $"The Bot doesn't have the required permissions. It needs: {botperm.Permissions}",
                RequireUserPermissionsAttribute userPerm => $"You don't have the required permissions. You need: {userPerm.Permissions}",
                RequirePermissionsAttribute perms => $"You or the bot don't the required permissions: {perms.Permissions}",
                RequireRolesAttribute roles => $"You need the following role(s) to use this command: {string.Join(", ",roles.RoleNames)}",
                RequireNsfwAttribute => $"This command can only be used in a nsfw channel!",
                CooldownAttribute cooldown => $"This command has a cooldown. Please wait {cooldown.GetRemainingCooldown(ctx).Humanize(culture: new("en-GB"))} before you can use it again.",
                _ => $"{check.TypeId} failed"
            };            
        }
    }
}