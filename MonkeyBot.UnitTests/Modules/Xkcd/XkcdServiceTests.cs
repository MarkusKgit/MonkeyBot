using MonkeyBot.Services;
using MonkeyBot.UnitTests.Utils;
using Moq;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace MonkeyBot.UnitTests.Modules.Xkcd
{
    public class XkcdServiceTests
    {
        private const string _baseUri = "https://xkcd.com/";
        private const string _infoJson = "info.0.json";

        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly IXkcdService xkcdService;

        public XkcdServiceTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();

            xkcdService = new XkcdService(_mockHttpClientFactory.Object);
        }

        [Fact(DisplayName = "Should get the latest comic")]
        public async Task ShouldGetLatestComic()
        {
            // Arrange
            var number = 69;
            var latestResponseHandler = PrepareMockResponseHandlerForLatest(number);
            var httpClient = PrepareHttpClient(latestResponseHandler);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var xkcdResponse = PrepareXkcdResponse(number);

            // Act
            var latestComic = await xkcdService.GetLatestComicAsync();

            // Assert
            PropertyMatcher.Match(xkcdResponse, latestComic);
        }

        [Fact(DisplayName = "Should return null on call failure")]
        public async Task ShouldReturnNullOnCallFail()
        {
            // Act
            var latestComic = await xkcdService.GetLatestComicAsync();

            // Assert
            Assert.Null(latestComic);
        }

        [Theory(DisplayName = "Should throw Argument Null Exception on invalid number")]
        [InlineData(-1)]
        [InlineData(404)]
        [InlineData(790)]
        public async Task ShouldthrowExceptionWhenInvalidNumberIsPassed(int number)
        {
            // Arrange
            var maxNumber = 690;
            var latestResponseHandler = PrepareMockResponseHandlerForLatest(maxNumber);
            var httpClient = PrepareHttpClient(latestResponseHandler);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            Func<Task> action = async () => await xkcdService.GetComicAsync(number);

            // Act and Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(action);
        }

        [Fact(DisplayName = "Should return the proper comic by number")]
        public async Task GetComicByNumber()
        {
            // Arrange
            var maxNumber = 690;
            var comicNumber = 420;
            var latestResponseHandler = PrepareMockResponseHandlerForLatest(maxNumber);
            PrepareMockResponseHandler(latestResponseHandler, comicNumber);
            var httpClient = PrepareHttpClient(latestResponseHandler);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var expectedComic = PrepareXkcdResponse(comicNumber);

            // Act
            var comic = await xkcdService.GetComicAsync(comicNumber);

            // Assert
            PropertyMatcher.Match(expectedComic, comic);
        }

        [Fact]
        public void ShouldreturnProperComicUrl()
        {
            // Arrange
            var comicNumber = 88;
            var expectedUri = _baseUri + comicNumber;

            // Act
            var comicUri = xkcdService.GetComicUrl(comicNumber);

            // Assert
            Assert.Equal(new Uri(expectedUri), comicUri);
        }

        private MockResponseHandler PrepareMockResponseHandlerForLatest(int number)
        {
            var mockResponseHandler = new MockResponseHandler();
            var uri = _baseUri + _infoJson;
            var xkcdResponse = PrepareXkcdResponse(number);
            mockResponseHandler.AddMockResponse(xkcdResponse, uri);

            return mockResponseHandler;
        }

        private void PrepareMockResponseHandler(MockResponseHandler mockResponseHandler, int number)
        {
            var uri = _baseUri + number + "/" + _infoJson;
            var xkcdResponse = PrepareXkcdResponse(number);
            mockResponseHandler.AddMockResponse(xkcdResponse, uri);
        }

        private static XkcdResponse PrepareXkcdResponse(int number)
        {
            return new XkcdResponse
            {
                Number = number,
                Month = "3",
                Link = "",
                Year = "2008",
                News = "",
                SafeTitle = "Convincing Pickup Line",
                Transcript = "",
                ImgUrl = new Uri(@"https://imgs.xkcd.com/comics/convincing_pickup_line.png"),
                Day = "31"
            };
        }

        private HttpClient PrepareHttpClient(MockResponseHandler mockResponseHandler)
        {
            return new HttpClient(mockResponseHandler);
        }
    }
}
