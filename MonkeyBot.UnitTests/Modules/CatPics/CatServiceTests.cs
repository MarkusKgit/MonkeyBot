using MonkeyBot.Services;
using MonkeyBot.UnitTests.Utils;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Xunit;

namespace MonkeyBot.UnitTests.Modules.CatPics
{
    public class CatServiceTests
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly ICatService catService;

        public CatServiceTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();

            catService = new CatService(_mockHttpClientFactory.Object);
        }

        [Theory(DisplayName = "Should return expected Picture Uri for specified breed")]
        [InlineData("Abyssinian", "abys")]
        [InlineData("Bengal", "beng")]
        public async Task GetExpectedPictureUriForSpecifiedBreed(string breed, string breedId)
        {
            // Arrange
            var httpClient = PrepareHttpClient(breed, breedId);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
            var expectedUri = "https://cdn2.thecatapi.com/images/" + breedId + ".jpg";

            // Act
            var uri = await catService.GetRandomPictureUrlAsync(breed);

            // Assert
            Assert.Equal(new Uri(expectedUri), uri);
        }

        [Theory(DisplayName = "Should return null for specified breed when more than one results are obtained")]
        [InlineData("Abyssinian", "abys")]
        [InlineData("Bengal", "beng")]
        public async Task GetNullPictureUriForSpecifiedBreed(string breed, string breedId)
        {
            // Arrange
            var httpClient = PrepareHttpClient(breed, breedId, 2);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Act
            var uri = await catService.GetRandomPictureUrlAsync(breed);

            // Assert
            Assert.Null(uri);
        }

        [Fact(DisplayName = "Should return null for specified breed when API call fails")]
        public async Task GetNullPictureUriForSpecifiedBreedOnException()
        {
            // Act
            var uri = await catService.GetRandomPictureUrlAsync();

            // Assert
            Assert.Null(uri);
        }

        [Fact(DisplayName = "Should return an empty list when API call fails")]
        public async Task ShouldReturEmptyListOnException()
        {
            // Act
            var breeds = await catService.GetBreedsAsync();

            // Assert
            Assert.False(breeds.Any());
        }

        [Fact(DisplayName = "Should return list of breeds")]
        public async Task ShouldReturnListOfBreeds()
        {
            // Arrange
            var expectedBreeds = new List<string> { "Abyssinian", "Bengal" };
            var httpClient = PrepareHttpClient(expectedBreeds);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Act
            var breeds = await catService.GetBreedsAsync();

            // Assert
            Assert.Equal(expectedBreeds, breeds);
        }

        private HttpClient PrepareHttpClient(string breedName, string breedId, int responseCount = 1)
        {
            var baseUri = "https://api.thecatapi.com/v1/";
            var breedFinderUri = $"{baseUri}breeds/search?q={HttpUtility.UrlEncode(breedName)}";
            var imageFinderUri = $"{baseUri}images/search?size=small&breed_id={breedId}";

            var mockMessageHandler = new MockResponseHandler();
            
            var catBreedResponse = Enumerable.Range(0, responseCount)
                .Select(c => new CatBreedsResponse(breedId, breedName))
                .ToList();
            mockMessageHandler.AddMockResponse(catBreedResponse, breedFinderUri);

            var catImageResponse = Enumerable.Range(0, responseCount)
                .Select(c => new CatResponse(new Uri("https://cdn2.thecatapi.com/images/" + breedId + ".jpg")))
                .ToList();
            mockMessageHandler.AddMockResponse(catImageResponse, imageFinderUri);

            return new HttpClient(mockMessageHandler);
        }

        private HttpClient PrepareHttpClient(List<string> breeds)
        {
            var baseUri = "https://api.thecatapi.com/v1/";
            var breedsUri = $"{baseUri}breeds";

            var mockMessageHandler = new MockResponseHandler();

            var catBreedResponse = breeds
                .Select(c => new CatBreedsResponse("", c))
                .ToList();
            mockMessageHandler.AddMockResponse(catBreedResponse, breedsUri);

            return new HttpClient(mockMessageHandler);
        }
    }
}
