using Discord.WebSocket;
using Fclp;
using Microsoft.Extensions.DependencyInjection;
using MonkeyBot;
using MonkeyBot.Common;
using System;
using System.Threading.Tasks;

public class Program
{
    private static IServiceProvider services;

    public static async Task Main(string[] args)
    {
        var parser = new FluentCommandLineParser<ApplicationArguments>();
        parser
            .Setup(arg => arg.BuildDocumentation)
            .As('d', "docu")
            .SetDefault(false)
            .WithDescription("Build the documentation files in the app folder");
        parser
            .SetupHelp("?", "help")
            .Callback(text => Console.WriteLine(text));
        var parseResult = parser.Parse(args);
        var parsedArgs = !parseResult.HasErrors ? parser.Object : null;

        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

        await DiscordClientConfiguration.EnsureExistsAsync(); // Ensure the configuration file has been created.

        services = await Initializer.InitializeAsync(parsedArgs);

        await Task.Delay(-1); // Prevent the console window from closing.
    }

    private static async void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            await Console.Out.WriteLineAsync($"Unhandled exception: {ex.Message}");

        if (e.IsTerminating)
            await Console.Out.WriteLineAsync("Terminating!");
    }

    private static async void CurrentDomain_ProcessExit(object sender, EventArgs e)
    {
        if (services != null)
        {
            var discordClient = services.GetService<DiscordSocketClient>();
            if (discordClient != null)
            {
                discordClient.LogoutAsync().Wait();
                discordClient.StopAsync().Wait();
            }
        }
        await Console.Out.WriteLineAsync("Exiting!");
    }
}