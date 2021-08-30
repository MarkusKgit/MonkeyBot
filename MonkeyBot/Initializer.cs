using DSharpPlus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonkeyBot.Database;
using MonkeyBot.Services;
using NLog.Config;
using NLog.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace MonkeyBot
{
    public static class Initializer
    {
        public static async Task InitializeServicesAsync(IServiceProvider services)
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

        public static IServiceProvider ConfigureServices(DiscordClient discordClient, LoggingConfiguration loggingConfiguration)
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

            IServiceProvider provider = new DefaultServiceProviderFactory().CreateServiceProvider(services);
            return provider;
        }
    }
}