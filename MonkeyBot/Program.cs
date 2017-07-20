using System;
using System.Threading.Tasks;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using MonkeyBot.Services;

public class Program
{
    private CommandService commands;
    private DiscordSocketClient client;
    private ServiceCollection serviceCollection;
    private IServiceProvider services;
    

    static void Main(string[] args) => new Program().Start().GetAwaiter().GetResult();

    public async Task Start()
    {
        client = new DiscordSocketClient();
        CommandServiceConfig commandConfig = new CommandServiceConfig();
        commandConfig.CaseSensitiveCommands = false;
        commandConfig.DefaultRunMode = RunMode.Async;
        commandConfig.LogLevel = LogSeverity.Error;
        commandConfig.ThrowOnError = false;        
        commands = new CommandService(commandConfig);
        commands = new CommandService();

        //string token = "***REMOVED***"; // Productive
        string token = "***REMOVED***"; // testing

        serviceCollection = new ServiceCollection();        
        serviceCollection.AddSingleton<IAnnouncementService>(new AnnouncementService());
        services = serviceCollection.BuildServiceProvider();

        await InstallCommands();

        client.UserJoined += Client_UserJoined;
        client.Connected += Client_Connected;
        client.Log += Client_Log;

        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();

        await Task.Delay(-1);
    }

    private Task Client_Log(LogMessage arg)
    {
        Console.WriteLine(arg.Message);
        return Task.CompletedTask;
    }

    private Task Client_Connected()
    {
        Console.WriteLine("Connected");
        return Task.CompletedTask;
    }

    private async Task Client_UserJoined(SocketGuildUser arg)
    {
        var channel = arg.Guild.DefaultChannel;
        await channel.SendMessageAsync("Hello there " + arg.Mention + "! Welcome to Monkey-Gamers. Read our welcome page for rules and info. If you have any issues feel free to contact our Admins or Leaders."); //Welcomes the new user
    }

    
    public async Task InstallCommands()
    {
        // Hook the MessageReceived Event into our Command Handler
        client.MessageReceived += HandleCommand;
        
        // Discover all of the commands in this assembly and load them.
        await commands.AddModulesAsync(Assembly.GetEntryAssembly());
    }

    public async Task HandleCommand(SocketMessage messageParam)
    {
        // Don't process the command if it was a System Message
        var message = messageParam as SocketUserMessage;
        if (message == null) return;
        // Create a number to track where the prefix ends and the command begins
        int argPos = 0;
        // Determine if the message is a command, based on if it starts with '!' or a mention prefix
        if (!(message.HasCharPrefix('!', ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos))) return;
        // Create a Command Context
        var context = new CommandContext(client, message);
        // Execute the command. (result does not indicate a return value, 
        // rather an object stating if the command executed successfully)
        var result = await commands.ExecuteAsync(context, argPos, services);
        if (!result.IsSuccess)
            await context.Channel.SendMessageAsync(result.ErrorReason);
    }
}
