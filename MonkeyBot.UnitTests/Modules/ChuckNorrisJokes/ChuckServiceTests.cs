using MonkeyBot.Services;
using MonkeyBot.UnitTests.Utils;
using Moq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace MonkeyBot.UnitTests.Modules.ChuckNorrisJokes
{
    public class ChuckServiceTests
    {
        private const string joke = "Chuck Norris doesn't need sudo, he just types \"Chuck Norris\" before his commands.";

        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly IChuckService chuckService;

        public ChuckServiceTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();

            chuckService = new ChuckService(_mockHttpClientFactory.Object);
        }

        [Fact(DisplayName = "Should return Chuck Norris Joke")]
        public async Task GetChuckJokeWithoutSpecifyingName()
        {
            // Arrange
            var jokeUri = "http://api.icndb.com/jokes/random";
            var httpClient = PrepareHttpClient(jokeUri, joke);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Act
            var chuckJoke = await chuckService.GetChuckFactAsync();

            // Assert
            Assert.Equal(joke, chuckJoke);
        }

        [Fact(DisplayName = "Should return Chuck Norris Joke - Name Specified")]
        public async Task GetChuckJokeAfterSpecifyingName()
        {
            // Arrange
            var firstName = "Chuck";
            var jokeUri = "http://api.icndb.com/jokes/random?firstName=" + firstName;
            var httpClient = PrepareHttpClient(jokeUri, joke);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Act
            var chuckJoke = await chuckService.GetChuckFactAsync(firstName);

            // Assert
            Assert.Equal(joke, chuckJoke);
        }

        [Fact(DisplayName = "Should return blank result on API Failure")]
        public async Task GetEmptyChuckJokeAfterOnAPIFailure()
        {
            // Act
            var chuckJoke = await chuckService.GetChuckFactAsync();

            // Assert
            Assert.True(string.IsNullOrWhiteSpace(chuckJoke));
        }

        [Fact(DisplayName = "Should return blank result on Empty Name")]
        public async Task GetEmptyChuckJokeOnEmptyName()
        {
            // Act
            var chuckJoke = await chuckService.GetChuckFactAsync("");

            // Assert
            Assert.True(string.IsNullOrWhiteSpace(chuckJoke));
        }

        private HttpClient PrepareHttpClient(string jokeUri, string joke)
        {
            var mockMessageHandler = new MockResponseHandler();

            var chuckResponse = new ChuckResponse("success", new ChuckJoke(joke));
            mockMessageHandler.AddMockResponse(chuckResponse, jokeUri);

            return new HttpClient(mockMessageHandler);
        }
    }
}
