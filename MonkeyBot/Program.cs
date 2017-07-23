using Discord;
using Discord.WebSocket;
using MonkeyBot;
using MonkeyBot.Common;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class Program
{
    private DiscordSocketClient client;
    private CommandHandler commands;

    private static void Main(string[] args) => new Program().StartAsync().GetAwaiter().GetResult();

    public async Task StartAsync()
    {
        Configuration.EnsureExists(); // Ensure the configuration file has been created.

        DiscordSocketConfig discordConfig = new DiscordSocketConfig(); //Create a new config for the Discord Client
        discordConfig.LogLevel = LogSeverity.Error;
        discordConfig.MessageCacheSize = 400;
        client = new DiscordSocketClient(discordConfig);    // Create a new instance of DiscordSocketClient with the specified config.

        client.Log += (l) => Console.Out.WriteLineAsync(l.ToString()); // Log to console for now

        HandleEvents(); //Add Event Handlers

        await client.LoginAsync(TokenType.Bot, Configuration.Load().ProductiveToken); // Log in to and start the bot client
        await client.StartAsync();

        commands = new CommandHandler(); // Initialize the command handler service
        await commands.InstallAsync(client);

        string docu = DocumentationBuilder.BuildHtmlDocumentation(commands.CommandService);
        string file = Path.Combine(AppContext.BaseDirectory, "documentation.txt");
        File.WriteAllText(file, docu);

        await Task.Delay(-1); // Prevent the console window from closing.
    }

    private void HandleEvents()
    {
        client.UserJoined += Client_UserJoined;
        client.Connected += Client_Connected;
    }

    private Task Client_Connected()
    {
        Console.WriteLine("Connected");
        return Task.CompletedTask;
    }

    private async Task Client_UserJoined(SocketGuildUser arg)
    {
        var channel = arg.Guild.DefaultChannel;
        await channel?.SendMessageAsync("Hello there " + arg.Mention + "! Welcome to Monkey-Gamers. Read our welcome page for rules and info. If you have any issues feel free to contact our Admins or Leaders."); //Welcomes the new user
    }
}