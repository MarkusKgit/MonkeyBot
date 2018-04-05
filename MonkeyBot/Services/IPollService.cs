using MonkeyBot.Services.Common.Poll;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IPollService
    {
        /// <summary>
        /// Start a new Poll
        /// </summary>
        /// <param name="poll"></param>
        /// <returns></returns>
        Task AddPollAsync(Poll poll);
    }
}