using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IPictureSearchService
    {
        Task<string> GetRandomPictureUrlAsync(string searchterm);
    }
}
