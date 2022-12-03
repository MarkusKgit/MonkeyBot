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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MonkeyBot.Modules.Reminders;
using System.Text;
using Microsoft.EntityFrameworkCore;

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

            // Surpress message clutter from entity framework and HTTP client
            logConfig.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Info, new NullTarget(), "Microsoft.*", final: true);
            logConfig.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Info, new NullTarget(), "System.Net.Http.*", final: true);
            
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
                        loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                        loggingBuilder.AddNLog(loggingConfiguration);                        
                    })                
                .AddHttpClient()
                .AddDbContextFactory<MonkeyDBContext>()
                .AddSingleton(discordClient)
                .AddSingleton<IGuildService, GuildService>()
                .AddSingleton<ISchedulingService, SchedulingService>()
                .AddSingleton<IReminderService, ReminderService>()
                .AddSingleton<IFeedService, FeedService>()
                .AddSingleton<IBattlefieldNewsService, BattlefieldNewsService>()
                .AddSingleton<IGiveAwaysService, GiveAwaysService>()
                .AddSingleton<SteamGameServerService>()
                .AddSingleton<MineCraftGameServerService>()
                .AddSingleton<IRoleDropdownService, RoleDropdownService>()
                .AddSingleton<IChuckService, ChuckService>()
                .AddSingleton<ICatService, CatService>()
                .AddSingleton<IDogService, DogService>()
                .AddSingleton<IXkcdService, XkcdService>()
                .AddSingleton<IPictureSearchService, GoogleImageSearchService>()
                .AddSingleton<ITriviaService, TriviaService>()
                .AddSingleton<IPollService, PollService>()
                .AddSingleton<IRoleManagementService, RoleManagementService>()
                .AddSingleton<IBenzenFactService, BenzenFactService>();

            IServiceProvider provider = new DefaultServiceProviderFactory().CreateServiceProvider(services);

            return provider;
        }

        private static async Task StartServicesAsync(IServiceProvider services)
        {
            IDbContextFactory<MonkeyDBContext> dbContextFactory = services.GetRequiredService<IDbContextFactory<MonkeyDBContext>>();
            MonkeyDBContext dbContext = dbContextFactory.CreateDbContext();
            await DBInitializer.InitializeAsync(dbContext);

            IReminderService reminders = services.GetService<IReminderService>();
            await reminders.InitializeAsync();

            SteamGameServerService steamGameServerService = services.GetService<SteamGameServerService>();
            steamGameServerService.Initialize();

            MineCraftGameServerService minecraftGameServerService = services.GetService<MineCraftGameServerService>();
            minecraftGameServerService.Initialize();

            IRoleDropdownService roleButtonsService = services.GetService<IRoleDropdownService>();
            await roleButtonsService.InitializeAsync();

            IFeedService feedService = services.GetService<IFeedService>();
            feedService.Start();

            IBattlefieldNewsService battlefieldNewsService = services.GetService<IBattlefieldNewsService>();
            battlefieldNewsService.Start();

            IGiveAwaysService giveAwaysService = services.GetService<IGiveAwaysService>();
            giveAwaysService.Start();

            IPollService pollService = services.GetService<IPollService>();
            //Disable for now and fix later
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
            commandsNext.SetHelpFormatter<MonkeyHelpFormatter>();
            commandsNext.RegisterCommands(Assembly.GetExecutingAssembly());
            commandsNext.CommandErrored += Commands_CommandErrored;

            return commandsNext;
        }

        private static async Task<int> HandlePrefixAsync(DiscordMessage msg)
        {
            string prefix = GuildConfig.DefaultPrefix;
            if (msg.Channel?.Guild != null)
            {
                prefix = await _guildService.GetPrefixForGuild(msg.Channel.Guild.Id);
            }
            return msg.GetStringPrefixLength(prefix);
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
            _ = Task.Run(async () =>
            {
                _discordClient.Logger.LogInformation("Client Connected");
                await _discordClient.UpdateStatusAsync(new DiscordActivity(@$"@{_discordClient.CurrentUser.Username} help", ActivityType.Watching));
            });
            return Task.CompletedTask;
        }

        private static Task DiscordClient_GuildDownloadCompleted(DiscordClient sender, GuildDownloadCompletedEventArgs e)
        {
            _ = Task.Run(() =>
            {
                var guildInfos = e.Guilds.Select(async g => await GetGuildInfo(g.Value)).Select(t => t.Result);
                string guildInfo = string.Join("\n", guildInfos);
                _discordClient.Logger.LogInformation($"Guild Download Complete:\n{guildInfo}");
            });
            return Task.CompletedTask;
        }

        private static async Task<string> GetGuildInfo(DiscordGuild guild)
        {
            var channels = await guild.GetChannelsAsync();
            string channelInfo = $"Text: {channels.Count(c => c.Type == ChannelType.Text)} Voice: {channels.Count(c => c.Type == ChannelType.Voice)} Other: {channels.Count(c => c.Type != ChannelType.Voice && c.Type != ChannelType.Text && c.Type != ChannelType.Category)}";            
            StringBuilder builder = new();
            builder.AppendLine($"{guild.Name}({guild.Id}):");
            builder.AppendLine($"├ Created on:    {guild.CreationTimestamp}");
            builder.AppendLine($"├ Owned by:      {guild.Owner.Username}");
            builder.AppendLine($"├ Description:   {guild.Description}");
            builder.AppendLine($"├ Membercount:   {guild.MemberCount}");            
            builder.AppendLine($"├ Channel count: {channelInfo}");
            builder.AppendLine($"└ Roles:         {string.Join(", ", guild.Roles.Values.OrderByDescending(r => r.Position).Select(r => r.Name))}");
            
            return builder.ToString();
        }

        private static Task DiscordClient_GuildMemberAdded(DiscordClient client, GuildMemberAddEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                GuildConfig config = await _guildService.GetOrCreateConfigAsync(e.Guild.Id);
                string welcomeMessage = config?.WelcomeMessageText ?? string.Empty;
                if (config?.WelcomeMessageChannelId != null && !welcomeMessage.IsEmpty())
                {
                    DiscordChannel channel = e.Guild.GetChannel(config.WelcomeMessageChannelId) ?? e.Guild.GetDefaultChannel();
                    welcomeMessage = welcomeMessage.Replace("%server%", e.Guild.Name).Replace("%user%", e.Member.DisplayName);
                    DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.DarkBlue)
                        .WithDescription(welcomeMessage)
                        .WithThumbnail(new Uri(e.Member.AvatarUrl ?? e.Member.DefaultAvatarUrl))
                        .WithTimestamp(DateTime.Now);

                    await (channel?.SendMessageAsync(embed: builder.Build()));
                }
            });
            return Task.CompletedTask;
        }

        private static Task DiscordClient_GuildMemberRemoved(DiscordClient client, GuildMemberRemoveEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                GuildConfig config = await _guildService.GetOrCreateConfigAsync(e.Guild.Id);
                string goodbyeMessage = config?.GoodbyeMessageText ?? string.Empty;
                if (config?.GoodbyeMessageChannelId != null && !goodbyeMessage.IsEmpty())
                {
                    DiscordChannel channel = e.Guild.GetChannel(config.WelcomeMessageChannelId) ?? e.Guild.GetDefaultChannel();
                    goodbyeMessage = goodbyeMessage.Replace("%server%", e.Guild.Name).Replace("%user%", e.Member.DisplayName);
                    DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.DarkBlue)
                        .WithDescription(goodbyeMessage)
                        .WithThumbnail(new Uri(e.Member.AvatarUrl ?? e.Member.DefaultAvatarUrl))
                        .WithTimestamp(DateTime.Now);

                    await (channel?.SendMessageAsync(embed: builder.Build()));
                }
            });
            return Task.CompletedTask;
        }

        private static Task DiscordClient_GuildMemberUpdated(DiscordClient client, GuildMemberUpdateEventArgs e)
        {
            _ = Task.Run(async () =>
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
                    await (channel?.SendMessageAsync($"{e.Member.DisplayName} has started streaming. Watch it [here]({e.Member.Presence.Activity.StreamUrl}) "));
                }
            });
            return Task.CompletedTask;
        }

        private static Task DiscordClient_GuildCreated(DiscordClient client, GuildCreateEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                _discordClient.Logger.LogInformation($"Joined guild:\n{await GetGuildInfo(e.Guild)}");
                // Make sure to create the config;
                await _guildService.GetOrCreateConfigAsync(e.Guild.Id);
            
                var msgBuilder = new DiscordEmbedBuilder()
                    .WithTitle("Hello, I am ***Monkey Bot***")
                    .WithThumbnail("https://raw.githubusercontent.com/MarkusKgit/MonkeyBot/main/Logos/MonkeyBot.png",50,50)
                    .WithColor(DiscordColor.SpringGreen)
                    .AddField("Documentation","https://github.com/MarkusKgit/MonkeyBot")
                    .AddField("Discord server", "https://discord.gg/hHyntGMh8E")
                    .WithDescription("Use command `!help` to check what can I do for you.");
            
                var channel = e.Guild.GetDefaultChannel() ?? await e.Guild.CreateTextChannelAsync("Welcome channel");
                await channel.SendMessageAsync(msgBuilder.Build());
            });
            return Task.CompletedTask;
        }

        private static Task DiscordClient_GuildDeleted(DiscordClient client, GuildDeleteEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                _discordClient.Logger.LogInformation($"Left guild {e.Guild.Name}");
                await _guildService.RemoveConfigAsync(e.Guild.Id);
            });
            return Task.CompletedTask;
        }

        private static async Task Commands_CommandErrored(CommandsNextExtension commandsNext, CommandErrorEventArgs e)
        {
            string prefix = GuildConfig.DefaultPrefix;
            if (e.Context.Guild != null)
            {
                prefix = await _guildService.GetPrefixForGuild(e.Context.Guild.Id);
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
            else if (e.Exception is DSharpPlus.Exceptions.UnauthorizedException unauthorizedException)
            {
                await e.Context.ErrorAsync(unauthorizedException.JsonMessage);
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
                RequireRolesAttribute roles => $"You need the following role(s) to use this command: {string.Join(", ", roles.RoleNames)}",
                RequireNsfwAttribute => $"This command can only be used in a nsfw channel!",
                CooldownAttribute cooldown => $"This command has a cooldown. Please wait {cooldown.GetRemainingCooldown(ctx).Humanize(culture: new("en-GB"))} before you can use it again.",
                _ => $"{check.TypeId} failed"
            };
        }
    }
}
