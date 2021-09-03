using System;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IPictureSearchService
    {
        Task<Uri> GetRandomPictureUrlAsync(string searchterm);
    }
}
