using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using MonkeyBot.Database;
using MonkeyBot.Models;
using MonkeyBot.Services;
using MonkeyBot.UnitTests.Utils;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MonkeyBot.UnitTests.Services
{
    public class GuildServiceTests
    {
        private readonly Mock<IDbContextFactory<MonkeyDBContext>> _monkeyDbContextFactory;
        private Mock<DbSet<GuildConfig>> _mockGuildConfig;
        private readonly IGuildService guildService;

        public GuildServiceTests()
        {
            var guildConfigs = PrepareGuildConfigs(100, 4);
            _monkeyDbContextFactory = new Mock<IDbContextFactory<MonkeyDBContext>>();
            _monkeyDbContextFactory.Setup(f => f.CreateDbContext())
                .Returns(() =>
                {
                    var context = new Mock<MonkeyDBContext>();
                    _mockGuildConfig = context.SetDbSetData(guildConfigs, c => c.GuildConfigs);
                    return context.Object;
                });
            
            guildService = new GuildService(_monkeyDbContextFactory.Object);
        }

        [Fact(DisplayName = "Should create GuildConfig if one does not exist for the given guildId")]
        public async Task CreateConfigIfOneDoesNotExist()
        {
            // Arrange
            var guildId = (ulong)1000L;
            var expectedResult = new GuildConfig { GuildID = guildId };

            // Act
            var result = await guildService.GetOrCreateConfigAsync(guildId);

            // Assert
            PropertyMatcher.Match(expectedResult, result);
            _mockGuildConfig.Verify(d => d.Add(Match.Create<GuildConfig>(g => g.GuildID == guildId)), Times.Once);
        }

        [Fact(DisplayName = "Should return existing GuildConfig if found")]
        public async Task ReturnExistingGuildConfigIfFound()
        {
            // Arrange
            var guildId = (ulong)100L;
            var expectedResult = new GuildConfig { GuildID = guildId, CommandPrefix = $"CommandPrefix-{guildId}" };

            // Act
            var result = await guildService.GetOrCreateConfigAsync(guildId);

            // Assert
            PropertyMatcher.Match(expectedResult, result);
            _mockGuildConfig.Verify(d => d.Add(It.IsAny<GuildConfig>()), Times.Never);
        }

        [Fact(DisplayName = "Should update GuildConfig")]
        public async Task ShouldUpdateGuildConfig()
        {
            // Arrange
            var guildId = (ulong)100L;
            var guildConfig = new GuildConfig { GuildID = guildId };

            // Act
            await guildService.UpdateConfigAsync(guildConfig);

            // Assert
            _mockGuildConfig.Verify(d => d.Update(Match.Create<GuildConfig>(g => g.GuildID == guildId)), Times.Once);            
        }

        [Fact(DisplayName = "Should remove GuildConfig if found")]
        public async Task ShouldRemoveGuildConfigIfFound()
        {
            // Arrange
            var guildId = (ulong)100L;

            // Act
            await guildService.RemoveConfigAsync(guildId);

            // Assert
            _mockGuildConfig.Verify(d => d.Remove(Match.Create<GuildConfig>(g => g.GuildID == guildId)), Times.Once);
        }

        [Fact(DisplayName = "Should not remove GuildConfig if not found")]
        public async Task ShouldNotRemoveGuildConfigIfNotFound()
        {
            // Arrange
            var guildId = (ulong)1000L;

            // Act
            await guildService.RemoveConfigAsync(guildId);

            // Assert
            _mockGuildConfig.Verify(d => d.Remove(Match.Create<GuildConfig>(g => g.GuildID == guildId)), Times.Never);
        }

        [Fact(DisplayName = "Should return the right CommandPrefix for an existing GuildConfig")]
        public async Task ShouldReturnTheRightPrefixForAnExistingGuildConfig()
        {
            // Arrange
            var guildId = (ulong)100L;

            // Act
            var result = await guildService.GetPrefixForGuild(guildId);

            // Assert
            Assert.Equal($"CommandPrefix-{guildId}", result);
        }

        [Fact(DisplayName = "Should return the default command prefix for new GuildConfig")]
        public async Task ShouldReturnTheDefaultCommandPrefixForNewGuildConfig()
        {
            // Arrange
            var guildId = (ulong)1000L;

            // Act
            var result = await guildService.GetPrefixForGuild(guildId);

            // Assert
            Assert.Equal(GuildConfig.DefaultPrefix, result);
        }

        private static List<GuildConfig> PrepareGuildConfigs(int startingValue, int count)
        {
            return Enumerable.Range(startingValue, count)
                .Select(gc => new GuildConfig { GuildID = (ulong)gc, CommandPrefix = $"CommandPrefix-{gc}" })
                .ToList();
        }
    }
}
