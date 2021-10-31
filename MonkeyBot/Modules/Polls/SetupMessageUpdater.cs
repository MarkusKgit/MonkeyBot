using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    public class SetupMessageUpdater
    {
        private static readonly TimeSpan _timeOut = TimeSpan.FromSeconds(60);
        
        private static readonly string introText =
            $"I will now guide you through the creation of the poll with a set of instructions.\n" +
            $"You have {_timeOut.TotalSeconds} seconds to answer each question \n" +
            $"Above you can see a preview of the poll that will get created \n\n";

        private const string FirstInstruction = "**1. Type a poll question**";
        private const string SecondInstruction = "**2. When should the poll end? Valid examples are:** \n" +
                                                 "*17:00* \n" +
                                                 "*Tomorrow 17:00* \n" +
                                                 "*Monday 17:00* \n";
        private const string ThirdInstruction =
            "**3. Now enter at least 2 poll answers by sending a message for each answer. Once you are done click \"👍\" to start the poll**";

        private static readonly DiscordEmoji _okEmoji = DiscordEmoji.FromUnicode("👍");
        
        private DiscordEmbedBuilder _setupEmbed;
        private DiscordMessage _setupMessage;

        public DiscordMessage Message => _setupMessage;

        private SetupMessageUpdater(DiscordEmbedBuilder messageEmbed,
            DiscordMessage message)
        {
            _setupEmbed = messageEmbed;
            _setupMessage = message;
        }
        
        public static async Task<SetupMessageUpdater> Create(CommandContext ctx)
        {
            var setupEmbed = new DiscordEmbedBuilder()
                .WithTitle("Poll Configuration")
                .WithColor(DiscordColor.Gold)
                .WithDescription(introText + FirstInstruction)
                .WithAuthor(ctx.Client.CurrentUser.Username, iconUrl: ctx.Client.CurrentUser.AvatarUrl);

            var setupMessage = await ctx.RespondAsync(embed: setupEmbed.Build());
            
            return new SetupMessageUpdater(setupEmbed, setupMessage);
        }

        public async Task SetSecondInstruction() => await WithDescription(introText + SecondInstruction);

        public async Task SetThirdInstruction() => await WithDescription(introText + ThirdInstruction);

        public async Task AddOkButton() =>
            await WithComponents(new DiscordButtonComponent(ButtonStyle.Primary, "btn_ok", "Start",
                emoji: new DiscordComponentEmoji(_okEmoji)));

        private async Task WithDescription(string description)
        {
            _setupEmbed = _setupEmbed.WithDescription(description);
            _setupMessage = await _setupMessage.ModifyAsync(embed: _setupEmbed.Build());
        }

        private async Task WithComponents(DiscordButtonComponent component)
        {
            var builder = new DiscordMessageBuilder()
                .WithEmbed(_setupEmbed.Build())
                .AddComponents(component);
            _setupMessage = await _setupMessage.ModifyAsync(builder);
        }
    }
}