using MonkeyBot;
using MonkeyBot.Common;
using System;
using System.Threading.Tasks;

public class Program
{
#pragma warning disable CC0061

    public static async Task Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        await Configuration.EnsureExistsAsync(); // Ensure the configuration file has been created.

        await Initializer.InitializeAsync();

        await Task.Delay(-1); // Prevent the console window from closing.
    }

    private static async void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            await Console.Out.WriteLineAsync($"Unhandled exception: {ex.Message}");

        if (e.IsTerminating)
            await Console.Out.WriteLineAsync("Terminating!");
    }

#pragma warning restore CC0061
}