using Discord;
using Discord.WebSocket;
using MonkeyBot;
using MonkeyBot.Common;
using System;
using System.IO;
using System.Threading.Tasks;

public class Program
{
    private DiscordSocketClient client;
    private CommandHandler commands;

    private static void Main(string[] args) => new Program().StartAsync().GetAwaiter().GetResult();

    public async Task StartAsync()
    {
        await Configuration.EnsureExistsAsync(); // Ensure the configuration file has been created.

        DiscordSocketConfig discordConfig = new DiscordSocketConfig(); //Create a new config for the Discord Client
        discordConfig.LogLevel = LogSeverity.Error;
        discordConfig.MessageCacheSize = 400;
        client = new DiscordSocketClient(discordConfig);    // Create a new instance of DiscordSocketClient with the specified config.

        client.Log += (l) => Console.Out.WriteLineAsync(l.ToString()); // Log to console for now

        HandleEvents(); //Add Event Handlers

        await client.LoginAsync(TokenType.Bot, (await Configuration.LoadAsync()).ProductiveToken); // Log in to and start the bot client
        await client.StartAsync();

        commands = new CommandHandler(); // Initialize the command handler service
        await commands.InstallAsync(client);

        string docu = await DocumentationBuilder.BuildHtmlDocumentationAsync(commands.CommandService);
        string file = Path.Combine(AppContext.BaseDirectory, "documentation.txt");
        await Helpers.WriteTextAsync(file, docu);

        await Task.Delay(-1); // Prevent the console window from closing.
    }

    private void HandleEvents()
    {
        client.UserJoined += Client_UserJoined;
        client.Connected += Client_Connected;
    }

    private async Task Client_Connected()
    {
        await Console.Out.WriteLineAsync("Connected");
    }

    private async Task Client_UserJoined(SocketGuildUser arg)
    {
        var channel = arg.Guild.DefaultChannel;
        await channel?.SendMessageAsync("Hello there " + arg.Mention + "! Welcome to Monkey-Gamers. Read our welcome page for rules and info or type !rules for a list of rules and !help for a list of commands you can use with our bot. If you have any issues feel free to contact our Admins or Leaders."); //Welcomes the new user
    }
}