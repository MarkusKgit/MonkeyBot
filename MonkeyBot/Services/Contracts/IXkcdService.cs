using MonkeyBot.Services.Common.Xkcd;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IXkcdService
    {
        /// <summary>
        /// Get the latest available xkcd comic
        /// </summary>
        /// <returns>Parsed json response of the xkcd API</returns>
        Task<XkcdResponse> GetLatestComicAsync();

        /// <summary>
        /// Get the xkcd comic with the specified number
        /// Throws <see cref="ArgumentOutOfRangeException"/> for invalid comic numbers
        /// </summary>
        /// <param name="comicNumber">A valid xkcd comic number</param>
        /// <returns>Parsed json response of the xkcd API</returns>
        Task<XkcdResponse> GetComicAsync(int comicNumber);

        /// <summary>
        /// Gets a random xkcd comic
        /// </summary>
        /// <returns>Parsed json response of the xkcd API</returns>
        Task<XkcdResponse> GetRandomComicAsync();

        /// <summary>
        /// Get the url of the comic specified by the comic number
        /// </summary>
        /// <param name="comicNumber">A valid xkcd comic number</param>
        /// <returns>Xkcd comic url</returns>
        Uri GetComicUrl(int comicNumber);
    }
}
