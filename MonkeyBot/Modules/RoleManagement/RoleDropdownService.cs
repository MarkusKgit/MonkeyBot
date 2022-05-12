using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonkeyBot.Database;
using MonkeyBot.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class RoleDropdownService : IRoleDropdownService
    {
        private readonly DiscordClient _discordClient;
        private readonly MonkeyDBContext _dbContext;
        private readonly ILogger<RoleDropdownService> _logger;
        private readonly IRoleManagementService _roleManagementService;
        // User Id -> previously selected roles
        private readonly ConcurrentDictionary<ulong, ulong[]> roleCache = new();

        private const string _message = "Please use this to assign yourself any role";

        public RoleDropdownService(DiscordClient discordClient, MonkeyDBContext dbContext, ILogger<RoleDropdownService> logger, IRoleManagementService roleManagementService)
        {
            _discordClient = discordClient;
            _dbContext = dbContext;
            _logger = logger;
            _roleManagementService = roleManagementService;
        }

        public async Task InitializeAsync()
        {
            _discordClient.ComponentInteractionCreated -= DiscordClient_ComponentInteractionCreated;
            _discordClient.ComponentInteractionCreated += DiscordClient_ComponentInteractionCreated;

            _discordClient.GuildRoleCreated -= DiscordClient_GuildRoleCreated;
            _discordClient.GuildRoleDeleted -= DiscordClient_GuildRoleDeleted;
            _discordClient.GuildRoleUpdated -= DiscordClient_GuildRoleUpdated;

            _discordClient.GuildRoleCreated += DiscordClient_GuildRoleCreated;
            _discordClient.GuildRoleDeleted += DiscordClient_GuildRoleDeleted;
            _discordClient.GuildRoleUpdated += DiscordClient_GuildRoleUpdated;

            await InitializeMessageComponentLinksAsync();
        }

        public async Task AddRoleSelectorComponentAsync(ulong guildId, ulong channelId, ulong messageId)
        {
            if (!_discordClient.Guilds.TryGetValue(guildId, out DiscordGuild guild))
            {
                _logger.LogDebug($"Error in {nameof(AddRoleSelectorComponentAsync)} - Guild was null");
                throw new ArgumentException("Invalid guild");
            }

            if (!guild.Channels.TryGetValue(channelId, out var channel))
            {
                _logger.LogDebug($"Error in {nameof(AddRoleSelectorComponentAsync)} - Channel not found");
                throw new ArgumentException("Invalid channel");
            }

            var message = await channel.GetMessageAsync(messageId);
            if (message == null)
            {
                _logger.LogDebug($"Error in {nameof(AddRoleSelectorComponentAsync)} - Message not found");
                throw new ArgumentException("Invalid message");
            }

            if (await ExistsAsync(guildId))
            {
                _logger.LogDebug($"Error in {nameof(AddRoleSelectorComponentAsync)} - Link already exists");
                throw new MessageComponentLinkAlreadyExistsException();
            }

            var roleSelectorComponent = await PrepareRoleSelectorDropdownComponentAndClearRoleCacheAsync(guild);

            var messageBuilder = new DiscordMessageBuilder().WithContent(_message).AddComponents(roleSelectorComponent);
            var roleSelectorComponentMessage = await message.RespondAsync(messageBuilder);

            var messageComponentLink = new MessageComponentLink { GuildId = guildId, ChannelId = channelId, ParentMessageId = messageId, MessageId = roleSelectorComponentMessage.Id, ComponentId = roleSelectorComponent.CustomId };
            _dbContext.MessageComponentLinks.Add(messageComponentLink);
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveRoleSelectorComponentsAsync(ulong guildId)
        {
            if (!_discordClient.Guilds.TryGetValue(guildId, out DiscordGuild guild))
            {
                _logger.LogDebug($"Error in {nameof(RemoveRoleSelectorComponentsAsync)} - Guild was null");
                throw new ArgumentException("Invalid guild");
            }

            var messageComponentLink = await _dbContext.MessageComponentLinks.FirstOrDefaultAsync(m => m.GuildId == guildId);
            if (messageComponentLink == null)
            {
                _logger.LogDebug($"Error in {nameof(RemoveRoleSelectorComponentsAsync)} - Could not get the message link");
                throw new MessageComponentLinkNotFoundException("No role selector component found in this guild");
            }

            await RemoveDatabaseEntryAsync(messageComponentLink);

            if (!guild.Channels.TryGetValue(messageComponentLink.ChannelId, out var channel))
            {
                _logger.LogDebug($"Error in {nameof(RemoveRoleSelectorComponentsAsync)} - Could not get the underlying channel");
                return;
            }

            var message = await channel.GetMessageAsync(messageComponentLink.MessageId);
            if (message == null)
            {
                _logger.LogDebug($"Error in {nameof(RemoveRoleSelectorComponentsAsync)} - Could not get the role selector component message");
                return;
            }

            await message.DeleteAsync();
        }

        public async Task<bool> ExistsAsync(ulong guildId)
            => await _dbContext.MessageComponentLinks.AnyAsync(x => x.GuildId == guildId);

        public async Task<MessageComponentLink> GetForGuildAsync(ulong guildId)
            => await _dbContext.MessageComponentLinks.SingleOrDefaultAsync(x => x.GuildId == guildId);

        private async Task RemoveDatabaseEntryAsync(MessageComponentLink messageComponentLink)
        {
            _dbContext.MessageComponentLinks.Remove(messageComponentLink);
            await _dbContext.SaveChangesAsync();
        }

        private async Task<DiscordSelectComponent> PrepareRoleSelectorDropdownComponentAndClearRoleCacheAsync(DiscordGuild guild)
        {
            roleCache.Clear();
            var assignableRoles = await _roleManagementService.GetAssignableRolesAsync(guild);
            var roleOptions = assignableRoles.Select(CreateSelectComponentOption);
            return new DiscordSelectComponent($"RoleDropdown-{guild.Id}", null, roleOptions, disabled: !roleOptions.Any(), minOptions: 0, maxOptions: roleOptions.Count());
        }

        private DiscordSelectComponentOption CreateSelectComponentOption(DiscordRole role)
            => new(role.Name, role.Id.ToString(), null, false);

        private Task DiscordClient_ComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs e)
        {
            Task.Run(async () =>
            {                
                var interactionUser = e.Interaction.User;
                var guild = e.Guild;
                var channel = e.Interaction.Channel;
                var message = e.Message;

                MessageComponentLink match = await _dbContext.MessageComponentLinks
                    .AsQueryable()
                    .SingleOrDefaultAsync(x => x.GuildId == guild.Id && x.ChannelId == message.Channel.Id && x.ComponentId == e.Id);
                if (match is not null)
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                    if (!roleCache.TryGetValue(interactionUser.Id, out ulong[] previouslySelectedRoles))
                    {
                        previouslySelectedRoles = Array.Empty<ulong>();
                    }
                    ulong[] selectedRoles = e.Values.Select(ulong.Parse).ToArray();
                    roleCache.AddOrUpdate(interactionUser.Id, selectedRoles, (_, __) => selectedRoles);
                    var addedRoles = selectedRoles.Except(previouslySelectedRoles).ToArray();
                    var removedRoles = previouslySelectedRoles.Except(selectedRoles).ToArray();
                    await AssignRoles(guild, interactionUser, addedRoles, removedRoles);
                }
            });
            return Task.CompletedTask;
        }

        private async Task AssignRoles(DiscordGuild guild, DiscordUser interactionUser, ulong[] adddedRoles, ulong[] removedRoles)
        {
            if (interactionUser.IsBot)
            {
                _logger.LogDebug($"Error in {nameof(AssignRoles)} - Reaction was triggered by a bot");
                return;
            }

            var interactionMember = await guild.GetMemberAsync(interactionUser.Id);

            foreach (var addedRoleId in adddedRoles)
            {                
                var addedRole = guild.GetRole(addedRoleId);

                if (addedRole == null)
                {
                    _logger.LogDebug($"Error in {nameof(AssignRoles)} - Invalid Role");
                    continue;
                }

                if (!interactionMember.Roles.Contains(addedRole))
                {
                    await interactionMember.GrantRoleAsync(addedRole);
                    await interactionMember.SendMessageAsync($"You now have the role {Formatter.Bold(addedRole.Name)} in {Formatter.Bold(interactionMember.Guild.Name)}!");
                }
            }

            foreach (var removedRoleId in removedRoles)
            {
                var removedRole = guild.GetRole(removedRoleId);

                if (removedRole == null)
                {
                    _logger.LogDebug($"Error in {nameof(AssignRoles)} - Invalid Role");
                    continue;
                }

                if (interactionMember.Roles.Contains(removedRole))
                {
                    await interactionMember.RevokeRoleAsync(removedRole);
                    await interactionMember.SendMessageAsync($"You now don't have the role {Formatter.Bold(removedRole.Name)} in {Formatter.Bold(interactionMember.Guild.Name)} any longer!");
                }
            }
        }

        private Task DiscordClient_GuildRoleDeleted(DiscordClient sender, GuildRoleDeleteEventArgs e)
        {
            Task.Run(async () => await UpdateRoleSelectorMessageAsync(e.Guild));
            return Task.CompletedTask;
        }

        private Task DiscordClient_GuildRoleCreated(DiscordClient sender, GuildRoleCreateEventArgs e)
        {
            Task.Run(async () => await UpdateRoleSelectorMessageAsync(e.Guild));
            return Task.CompletedTask;
        }

        private Task DiscordClient_GuildRoleUpdated(DiscordClient sender, GuildRoleUpdateEventArgs e)
        {
            Task.Run(async () => await UpdateRoleSelectorMessageAsync(e.Guild));
            return Task.CompletedTask;
        }

        private async Task UpdateRoleSelectorMessageAsync(DiscordGuild guild, MessageComponentLink messageComponentLink = null)
        {
            messageComponentLink ??= await _dbContext.MessageComponentLinks.SingleOrDefaultAsync(m => m.GuildId == guild.Id);
            var messageId = messageComponentLink.MessageId;
            var channelId = messageComponentLink.ChannelId;
            DiscordChannel channel = guild.GetChannel(channelId);
            if (channel == null)
            {
                throw new ArgumentException("Invalid channel");
            }
            var roleSelectorComponent = await PrepareRoleSelectorDropdownComponentAndClearRoleCacheAsync(guild);            
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

        private async Task InitializeMessageComponentLinksAsync()
        {
            var messageComponentLinks = await _dbContext.MessageComponentLinks.ToListAsync();
            foreach (var messageComponentLink in messageComponentLinks)
            {
                var exists = await LinkExists(messageComponentLink);
                if (exists == true && _discordClient.Guilds.TryGetValue(messageComponentLink.GuildId, out DiscordGuild guild))
                {
                    await UpdateRoleSelectorMessageAsync(guild, messageComponentLink);
                }
                else
                {
                    await RemoveDatabaseEntryAsync(messageComponentLink);
                }
            }
        }

        private async Task<bool?> LinkExists(MessageComponentLink messageComponentLink)
        {
            if (!_discordClient.Guilds.TryGetValue(messageComponentLink.GuildId, out DiscordGuild guild))
            {
                return false;
            }

            try
            {
                var channel = guild.GetChannel(messageComponentLink.ChannelId);
                if (channel == null)
                {
                    return false;
                }

                var message = await channel.GetMessageAsync(messageComponentLink.MessageId);
                if (message == null)
                {
                    return false;
                }
            }
            catch (ServerErrorException) { return null; } // Thrown when Discord is unable to process the request. Therefore we do not have sufficient information to determine whether to update the message component or to remove it.
            catch (NotFoundException) { return false; }

            return true;
        }
    }
}