using Microsoft.Extensions.DependencyInjection;
using MonkeyBot;
using MonkeyBot.Common;
using MonkeyBot.Services;
using System.Threading.Tasks;

public class Program
{
    private static void Main(string[] args) => new Program().StartAsync().GetAwaiter().GetResult();

    public async Task StartAsync()
    {
        await Configuration.EnsureExistsAsync(); // Ensure the configuration file has been created.

        var services = await Initializer.InitializeAsync();

        var manager = services.GetService<CommandManager>();
        await manager.StartAsync();

        var eventHandler = services.GetService<EventHandlerService>();
        eventHandler.Start();

        var announcements = services.GetService<IAnnouncementService>();
        await announcements.InitializeAsync();

        var backgroundTasks = services.GetService<IBackgroundService>();
        backgroundTasks.Start();

        await manager.BuildDocumentationAsync(); // Write the documentation

        await Task.Delay(-1); // Prevent the console window from closing.
    }
}