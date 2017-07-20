using System;
using System.Threading.Tasks;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;

public class Program
{
    private CommandService commands;
    private DiscordSocketClient client;
    private IServiceProvider services;

    static void Main(string[] args) => new Program().Start().GetAwaiter().GetResult();

    public async Task Start()
    {
        client = new DiscordSocketClient();
        commands = new CommandService();

        //string token = "MzMzOTU5NTcwNDM3MTc3MzU0.DEUP0w.D0OZDsaSJK9U3tnoYTDpveX0zB8"; // Productive
        string token = "MzM3MjkyMTYzMjMyNTYzMjA1.DFEvVg.1ZI2GSgHk34aqAbCXhpHuuS5XsQ"; // testing

        services = new ServiceCollection()
                .BuildServiceProvider();

        await InstallCommands();

        client.UserJoined += Client_UserJoined;

        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();

        await Task.Delay(-1);
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
