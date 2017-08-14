using MonkeyBot;
using MonkeyBot.Common;
using System.Threading.Tasks;

public class Program
{
    private static void Main(string[] args) => new Program().StartAsync().GetAwaiter().GetResult();

    public async Task StartAsync()
    {
        await Configuration.EnsureExistsAsync(); // Ensure the configuration file has been created.

        await Initializer.InitializeAsync();

        await Task.Delay(-1); // Prevent the console window from closing.
    }
}