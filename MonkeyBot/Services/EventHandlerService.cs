using Discord.WebSocket;
using dokas.FluentStrings;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class EventHandlerService
    {
        private readonly DbService dbService;
        private readonly DiscordSocketClient discordClient;

        public EventHandlerService(DbService db, DiscordSocketClient client)
        {
            this.dbService = db;
            this.discordClient = client;
        }

        public void Start()
        {
            discordClient.UserJoined += Client_UserJoinedAsync;
            discordClient.Connected += Client_ConnectedAsync;
        }

        private async static Task Client_ConnectedAsync()
        {
            await Console.Out.WriteLineAsync("Connected");
        }

        private async Task Client_UserJoinedAsync(SocketGuildUser arg)
        {
            if (arg.Guild == null)
                return;
            var channel = arg.Guild.DefaultChannel;
            string welcomeMessage = string.Empty;
            using (var uow = dbService.UnitOfWork)
            {
                welcomeMessage = (await uow.GuildConfigs.GetAsync(arg.Guild.Id))?.WelcomeMessageText;
            }
            if (!welcomeMessage.IsEmpty())
            {
                welcomeMessage = welcomeMessage.Replace("%server%", arg.Guild.Name);
                welcomeMessage = welcomeMessage.Replace("%user%", arg.Mention);
                await channel?.SendMessageAsync(welcomeMessage);
            }
        }
    }
}