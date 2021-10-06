using MonkeyBot.Services;
using MonkeyBot.UnitTests.Utils;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace MonkeyBot.UnitTests.Modules.DogPics
{
    public class DogServiceTests
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly IDogService dogService;

        public DogServiceTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();

            dogService = new DogService(_mockHttpClientFactory.Object);
        }

        [Fact(DisplayName = "Should return a random image uri for the specified breed")]
        public async Task ShouldReturnRandomImageUriForBreed()
        {
            // Arrange
            var breed = "african";
            var httpClient = PrepareHttpClient(breed);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
            var expectedUri = "https://images.dog.ceo/breeds/" + breed + ".jpg";

            // Act
            var uri = await dogService.GetRandomPictureUrlAsync(breed);

            // Assert
            Assert.Equal(new Uri(expectedUri), uri);
        }

        [Fact(DisplayName = "Should return a random image uri when no breed is specified")]
        public async Task ShouldReturnRandomImageUri()
        {
            // Arrange
            var httpClient = PrepareHttpClient();
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
            var expectedUri = "https://images.dog.ceo/breeds/random.jpg";

            // Act
            var uri = await dogService.GetRandomPictureUrlAsync();

            // Assert
            Assert.Equal(new Uri(expectedUri), uri);
        }

        [Fact(DisplayName = "Should return null image uri when Api call fails")]
        public async Task ShouldReturnNullImageUriWhenCallFails()
        {
            // Act
            var uri = await dogService.GetRandomPictureUrlAsync();

            // Assert
            Assert.Null(uri);
        }

        [Fact(DisplayName = "Should return a list of breeds")]
        public async Task ShouldReturnListOfBreeds()
        {
            // Arrange
            var expectedBreeds = new List<string> { "Australian", "Beagle" };
            var httpClient = PrepareHttpClient(expectedBreeds);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Act
            var breeds = await dogService.GetBreedsAsync();

            // Assert
            Assert.Equal(expectedBreeds, breeds);
        }

        [Fact(DisplayName = "Should return an empty list when API call fails")]
        public async Task ShouldReturEmptyListOnException()
        {
            // Act
            var breeds = await dogService.GetBreedsAsync();

            // Assert
            Assert.False(breeds.Any());
        }

        private HttpClient PrepareHttpClient(List<string> breeds)
        {
            var baseUri = "https://dog.ceo/api/";
            var breedsUri = $"{baseUri}breeds/list/all";

            var mockMessageHandler = new MockResponseHandler();

            var dogBreedResponse = new DogBreedsResponse("success", breeds.ToDictionary(k => k, k => Enumerable.Empty<string>().ToList()));
            mockMessageHandler.AddMockResponse(dogBreedResponse, breedsUri);

            return new HttpClient(mockMessageHandler);
        }

        private HttpClient PrepareHttpClient(string breed)
        {
            var baseUri = "https://dog.ceo/api/";
            var breedsUri = $"{baseUri}breed/{breed}/images/random";

            var mockMessageHandler = new MockResponseHandler();

            var dogBreedResponse = new DogResponse("success", "https://images.dog.ceo/breeds/" + breed + ".jpg");
            mockMessageHandler.AddMockResponse(dogBreedResponse, breedsUri);

            return new HttpClient(mockMessageHandler);
        }

        private HttpClient PrepareHttpClient()
        {
            var baseUri = "https://dog.ceo/api/";
            var breedsUri = $"{baseUri}breeds/image/random";

            var mockMessageHandler = new MockResponseHandler();

            var dogBreedResponse = new DogResponse("success", "https://images.dog.ceo/breeds/random.jpg");
            mockMessageHandler.AddMockResponse(dogBreedResponse, breedsUri);

            return new HttpClient(mockMessageHandler);
        }
    }
}
