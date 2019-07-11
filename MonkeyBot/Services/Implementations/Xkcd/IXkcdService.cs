using MonkeyBot.Services.Common.Xkcd;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IXkcdService
    {
        Task<XkcdResponse> GetLatestComicAsync();

        Task<XkcdResponse> GetComicAsync(int comicNumber);

        Task<XkcdResponse> GetRandomComicAsync();

        Uri GetComicUrl(int comicNumber);
    }
}
