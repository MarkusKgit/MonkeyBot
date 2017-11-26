using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IChuckService
    {
        Task<string> GetChuckFactAsync();

        Task<string> GetChuckFactAsync(string userName);
    }
}