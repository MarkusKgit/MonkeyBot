using DSharpPlus;
using DSharpPlus.CommandsNext;
using Fclp;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MonkeyBot
{
    public static class Program
    {
        private static DiscordClient _discordClient;

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
            ICommandLineParserResult parseResult = parser.Parse(args);
            ApplicationArguments parsedArgs = !parseResult.HasErrors ? parser.Object : null;
            
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            var services = await Initializer.InitializeServicesAndStartClientAsync();
            _discordClient = services.GetRequiredService<DiscordClient>();

            if (parsedArgs.BuildDocumentation)
            {
                var docs = Documentation.DocumentationBuilder.BuildDocumentation(_discordClient.GetCommandsNext(), Documentation.DocumentationOutputType.HTML);
                await File.WriteAllTextAsync(@"C:\temp\commands.html", docs);
                await Console.Out.WriteLineAsync("Documentation built");
            }

            await Task.Delay(-1); // Prevent the console window from closing.
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