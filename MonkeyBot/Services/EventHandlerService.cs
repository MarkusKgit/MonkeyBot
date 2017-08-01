using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class EventHandlerService
    {
        private DbService db;
        private DiscordSocketClient client;

        public EventHandlerService(IServiceProvider provider)
        {
            db = provider.GetService<DbService>();
            client = provider.GetService<DiscordSocketClient>();
        }

        public void Start()
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
            if (arg.Guild == null)
                return;
            var channel = arg.Guild.DefaultChannel;
            string welcomeMessage = string.Empty;
            using (var uow = db.UnitOfWork)
            {
                welcomeMessage = (await uow.GuildConfigs.GetOrCreateAsync(arg.Guild.Id)).WelcomeMessageText;
            }
            welcomeMessage = welcomeMessage.Replace("%server%", arg.Guild.Name);
            welcomeMessage = welcomeMessage.Replace("%user%", arg.Mention);
            await channel?.SendMessageAsync(welcomeMessage);
            //await channel?.SendMessageAsync("Hello there " + arg.Mention + "! Welcome to Monkey-Gamers. Read our welcome page for rules and info or type !rules for a list of rules and !help for a list of commands you can use with our bot. If you have any issues feel free to contact our Admins or Leaders.");
        }
    }
}