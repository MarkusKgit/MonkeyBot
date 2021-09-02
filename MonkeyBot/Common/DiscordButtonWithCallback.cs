using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Common
{
    public class DiscordButtonWithCallback
    {
        private DiscordButtonComponent _button;
        private DiscordClient _client;
        private Func<DiscordInteractionResponseBuilder> _response;
        private Func<DiscordFollowupMessageBuilder> _followup;

        public DiscordButtonComponent Button => _button;

        private DiscordButtonWithCallback(DiscordClient client, DiscordButtonComponent button)
        {
            _client = client;
            _button = button;
            _client.ComponentInteractionCreated += HandleComponentInteractionCreated;            
        }

        public DiscordButtonWithCallback(DiscordClient client, DiscordButtonComponent button, Func<DiscordInteractionResponseBuilder> response) : this(client, button)
        {
            _response = response;
        }

        public DiscordButtonWithCallback(DiscordClient client, ButtonStyle style, string customId, string label, DiscordComponentEmoji emoji, Func<DiscordInteractionResponseBuilder> response) : this(client, new DiscordButtonComponent(style, customId, label, emoji: emoji))
        {
            _response = response;
        }

        public DiscordButtonWithCallback(DiscordClient client, DiscordButtonComponent button, Func<DiscordFollowupMessageBuilder> followup) : this(client, button)
        {
            _followup = followup;
        }

        public DiscordButtonWithCallback(DiscordClient client, ButtonStyle style, string customId, string label, DiscordComponentEmoji emoji, Func<DiscordFollowupMessageBuilder> followup) : this(client, new DiscordButtonComponent(style, customId, label, emoji: emoji))
        {
            _followup = followup;
        }

        private async Task HandleComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs e)
        {
            if (e.Id != _button.CustomId)
            {
                return;
            }

            if (_response != null)
            {
                var responseBuilder = _response();
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, responseBuilder);
            }
            else if (_followup != null)
            {
                var followupMessageBuilder = _followup();
                await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                await e.Interaction.CreateFollowupMessageAsync(followupMessageBuilder);
            }
        }

        public static implicit operator DiscordButtonComponent(DiscordButtonWithCallback d) => d.Button;
    }
}
