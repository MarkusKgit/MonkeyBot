using MonkeyBot.Models;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IPollService
    {
        Task AddAndStartPollAsync(Poll poll);

        Task InitializeAsync();
    }
}
