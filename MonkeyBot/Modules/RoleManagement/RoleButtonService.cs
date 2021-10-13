using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonkeyBot.Database;
using MonkeyBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class RoleButtonService : IRoleButtonService
    {
        private readonly DiscordClient _discordClient;
        private readonly MonkeyDBContext _dbContext;
        private readonly ILogger<RoleButtonService> _logger;
        private readonly IRoleManagementService _roleManagementService;

        private const string _assignableRoleDropDownId = "assignableRoles-";
        private const string _message = "Please use this to assign yourself any role";
        private const string _removedMessage = "Removed";

        public RoleButtonService(DiscordClient discordClient, MonkeyDBContext dbContext, ILogger<RoleButtonService> logger, IRoleManagementService roleManagementService)
        {
            _discordClient = discordClient;
            _dbContext = dbContext;
            _logger = logger;
            _roleManagementService = roleManagementService;
        }

        public void Initialize()
        {
            _discordClient.ComponentInteractionCreated -= DiscordClient_ComponentInteractionCreated;
            _discordClient.ComponentInteractionCreated += DiscordClient_ComponentInteractionCreated;

            _discordClient.GuildRoleCreated -= DiscordClient_GuildRoleCreated;
            _discordClient.GuildRoleDeleted -= DiscordClient_GuildRoleDeleted;
            _discordClient.GuildRoleUpdated -= DiscordClient_GuildRoleUpdated;

            _discordClient.GuildRoleCreated += DiscordClient_GuildRoleCreated;
            _discordClient.GuildRoleDeleted += DiscordClient_GuildRoleDeleted;
            _discordClient.GuildRoleUpdated += DiscordClient_GuildRoleUpdated;
        }

        public async Task AddRoleSelectorComponentAsync(ulong guildId, ulong channelId, ulong messageId, DiscordUser botUser)
        {
            if (!_discordClient.Guilds.TryGetValue(guildId, out DiscordGuild guild))
            {
                throw new ArgumentException("Invalid guild");
            }

            var channel = guild.GetChannel(channelId);
            if (channel == null)
            {
                throw new ArgumentException("Invalid channel");
            }

            var message = await channel.GetMessageAsync(messageId);
            if (message == null)
            {
                throw new ArgumentException("Invalid message");
            }

            var roleSelectorComponent = await PrepareRoleSelectorDropdownComponent(botUser, guild, message.Id);

            var messageBuilder = new DiscordMessageBuilder().WithContent(_message).AddComponents(roleSelectorComponent);
            var roleSelectorComponentMessage = await message.RespondAsync(messageBuilder);

            var messageComponentLink = new MessageComponentLink { GuildId = guildId, ChannelId = channelId, ParentMessageId = messageId, MessageId = roleSelectorComponentMessage.Id, ComponentId = roleSelectorComponent.CustomId };
            _dbContext.MessageComponentLinks.Add(messageComponentLink);
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveRoleSelectorComponentsAsync(ulong guildId, ulong channelId, ulong messageId)
        {
            if (!_discordClient.Guilds.TryGetValue(guildId, out DiscordGuild guild))
            {
                _logger.LogDebug($"Error in {nameof(RemoveRoleSelectorComponentsAsync)} of {nameof(RoleButtonService)} - Guild was null");
                return;
            }

            var channel = guild.GetChannel(channelId);
            if (channel == null)
            {
                _logger.LogDebug($"Error in {nameof(RemoveRoleSelectorComponentsAsync)} of {nameof(RoleButtonService)} - Could not get the underlying channel");
                return;
            }

            var messageComponentLink = await _dbContext.MessageComponentLinks.FirstOrDefaultAsync(m => m.GuildId == guildId && m.ChannelId == channelId && m.ParentMessageId == messageId);
            if (messageComponentLink == null)
            {
                _logger.LogDebug($"Error in {nameof(RemoveRoleSelectorComponentsAsync)} of {nameof(RoleButtonService)} - Could not get the message");
                return;
            }

            var message = await channel.GetMessageAsync(messageComponentLink.MessageId);
            if (message == null)
            {
                _logger.LogDebug($"Error in {nameof(RemoveRoleSelectorComponentsAsync)} of {nameof(RoleButtonService)} - Could not get the role selector component message");
                return;
            }

            await message.ModifyAsync(builder => builder.WithContent(_removedMessage).ClearComponents());

            _dbContext.MessageComponentLinks.Remove(messageComponentLink);

            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveAllRoleSelectorComponentsAsync(ulong guildId)
        {
            if (!_discordClient.Guilds.TryGetValue(guildId, out DiscordGuild guild))
            {
                _logger.LogDebug($"Error in {nameof(RemoveAllRoleSelectorComponentsAsync)} of {nameof(RoleButtonService)} - Guild was null");
                return;
            }

            var messageComponentLinks = await _dbContext.MessageComponentLinks.Where(m => m.GuildId == guildId).ToListAsync();
            try
            {
                for (var index = 0; index < messageComponentLinks.Count; index++)
                {
                    var messageComponentLink = messageComponentLinks.ElementAt(index);
                    var channel = guild.GetChannel(messageComponentLink.ChannelId);
                    if (channel == null)
                    {
                        _logger.LogDebug($"Error in {nameof(RemoveAllRoleSelectorComponentsAsync)} of {nameof(RoleButtonService)} - Could not get the underlying channel");
                        continue;
                    }

                    var message = await channel.GetMessageAsync(messageComponentLink.MessageId);
                    if (message == null)
                    {
                        _logger.LogDebug($"Error in {nameof(RemoveAllRoleSelectorComponentsAsync)} of {nameof(RoleButtonService)} - Could not get the underlying message");
                        continue;
                    }

                    await message.ModifyAsync(builder => builder.WithContent(_removedMessage).ClearComponents());

                    _dbContext.MessageComponentLinks.Remove(messageComponentLink);
                }
            }
            finally
            {
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(ulong guildID, ulong channelId, ulong messageId)
        {
            var linkCount = await _dbContext.MessageComponentLinks
                .AsQueryable()
                .CountAsync(x => x.GuildId == guildID && x.ChannelId == channelId && x.ParentMessageId == messageId);
            return linkCount > 0;
        }

        public async Task<string> ListAllAsync(ulong guildID)
        {
            var links = await _dbContext.MessageComponentLinks
                .AsQueryable()
                .Where(x => x.GuildId == guildID)
                .ToListAsync();
            if (links == null || links.Count < 1)
            {
                return "";
            }

            var sb = new StringBuilder();
            foreach (var link in links)
            {
                if (_discordClient.Guilds.TryGetValue(link.GuildId, out DiscordGuild guild)
                    && guild.GetChannel(link.ChannelId) is DiscordChannel channel
                    && (await channel.GetMessageAsync(link.ParentMessageId)) is DiscordMessage message)
                {
                    sb.AppendLine($"Message Id: [{link.ParentMessageId}]({message.JumpLink})");
                }
            }
            return sb.ToString();
        }

        private async Task<DiscordSelectComponent> PrepareRoleSelectorDropdownComponent(DiscordUser botUser, DiscordGuild guild, ulong messageId)
        {
            var botRole = await _roleManagementService.GetBotRoleAsync(botUser, guild);
            var assignableRoles = _roleManagementService.GetAssignableRoles(botRole, guild);

            return PrepareRoleSelectorDropdownComponent(assignableRoles, messageId);
        }

        private DiscordSelectComponent PrepareRoleSelectorDropdownComponent(IEnumerable<DiscordRole> assignableRoles, ulong messageId) => PrepareRoleSelectorDropdownComponent(assignableRoles, _assignableRoleDropDownId + messageId);

        private DiscordSelectComponent PrepareRoleSelectorDropdownComponent(IEnumerable<DiscordRole> assignableRoles, string dropdownId)
        {
            var roleOptions = assignableRoles.Select(CreateSelectComponentOption);
            return new DiscordSelectComponent(dropdownId, null, roleOptions, disabled: !roleOptions.Any(), maxOptions: roleOptions.Count());
        }

        private DiscordSelectComponentOption CreateSelectComponentOption(DiscordRole role)
            => new(role.Name, role.Id.ToString(), null, false);

        private async Task DiscordClient_ComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs e)
        {
            var interactionUser = e.Interaction.User;
            var guild = e.Guild;
            var channel = e.Interaction.Channel;
            var message = e.Message;

            MessageComponentLink match = await _dbContext.MessageComponentLinks
                .AsQueryable()
                .SingleOrDefaultAsync(x => x.GuildId == guild.Id && x.ChannelId == message.Channel.Id && x.ComponentId == e.Id);
            if (match is object && e.Values.Any())
            {
                await AssignRole(sender, guild, interactionUser, e.Values, e.Interaction);
            }
        }

        private async Task AssignRole(DiscordClient client, DiscordGuild guild, DiscordUser interactionUser, string[] selectedRoleIds, DiscordInteraction interaction)
        {
            if (interactionUser.IsBot)
            {
                _logger.LogDebug($"Error in {nameof(AssignRole)} of {nameof(RoleButtonService)} - Reaction was triggered by a bot");
                return;
            }

            var interactionMember = await guild.GetMemberAsync(interactionUser.Id);

            foreach (var selectedRoleId in selectedRoleIds)
            {
                if (ulong.TryParse(selectedRoleId, out var roleId))
                {
                    var role = guild.GetRole(roleId);

                    if (role == null)
                    {
                        _logger.LogDebug($"Error in {nameof(AssignRole)} of {nameof(RoleButtonService)} - Invalid Role");
                        continue;
                    }

                    if (interactionMember.Roles.Contains(role))
                    {
                        await interactionMember.SendMessageAsync($"You already are a {role.Name} {interactionMember.DisplayName}!");
                    }
                    else
                    {
                        await interactionMember.GrantRoleAsync(role);
                        await interactionMember.SendMessageAsync($"You're a {role.Name} {interactionMember.DisplayName}!");
                    }

                    await interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                }
                else
                {
                    _logger.LogDebug($"Error in {nameof(AssignRole)} of {nameof(RoleButtonService)} - Could not find the selected role");
                }
            }

        }

        private async Task DiscordClient_GuildRoleDeleted(DiscordClient sender, GuildRoleDeleteEventArgs e) =>
            await UpdateRoleSelectorsAsync(e.Guild);

        private async Task DiscordClient_GuildRoleCreated(DiscordClient sender, GuildRoleCreateEventArgs e) =>
            await UpdateRoleSelectorsAsync(e.Guild);

        private async Task DiscordClient_GuildRoleUpdated(DiscordClient sender, GuildRoleUpdateEventArgs e) =>
            await UpdateRoleSelectorsAsync(e.Guild);

        private async Task UpdateRoleSelectorsAsync(DiscordGuild guild)
        {
            var roles = guild.Roles.Select(d => d.Value).Where(role => role.IsMentionable
                               && role != guild.EveryoneRole
                               && !role.Permissions.HasFlag(Permissions.Administrator)).ToList();
            var messageComponentLinks = await _dbContext.MessageComponentLinks.Where(m => m.GuildId == guild.Id).ToListAsync();
            foreach (var messageComponentLink in messageComponentLinks)
            {
                await UpdateRoleSelectorDropdownComponentAsync(guild, roles, messageComponentLink);
            }
        }

        private async Task UpdateRoleSelectorDropdownComponentAsync(DiscordGuild guild, IEnumerable<DiscordRole> roles, MessageComponentLink messageComponentLink)
        {
            var messageId = messageComponentLink.MessageId;
            var channelId = messageComponentLink.ChannelId;
            DiscordChannel channel = guild.GetChannel(channelId);
            if (channel == null)
            {
                throw new ArgumentException("Invalid channel");
            }
            var roleSelectorComponent = PrepareRoleSelectorDropdownComponent(roles, messageComponentLink.ComponentId);
            DiscordMessage message = await channel.GetMessageAsync(messageId);
            if (message == null)
            {
                throw new ArgumentException("Invalid message");
            }

            await message.ModifyAsync(builder =>
            {
                builder.ClearComponents();
                builder.WithContent(_message).AddComponents(roleSelectorComponent);
            });
        }
    }
}