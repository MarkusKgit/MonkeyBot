using MonkeyBot.Services.Common.Poll;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IPollService
    {
        Task AddPollAsync(Poll poll);
    }
}