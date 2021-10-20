using System.Threading.Tasks;

namespace MonkeyBot.Services
{ 
    /// <summary>
    /// Service to get and add Facts about Benzen
    /// </summary>
    public interface IBenzenFactService
    {
        /// <summary>
        /// Get a random Benzen Fact
        /// </summary>
        /// <returns>Fact and its position</returns>
        public Task<(string Fact, int Number)> GetRandomFactAsync();
        
        /// <summary>
        /// Add a new Benzen fact. Must contain the keyword benzen
        /// </summary>
        /// <param name="fact"></param>
        /// <returns></returns>
        public Task AddFactAsync(string fact);
        
        /// <summary>
        /// Check if the specified fact already exists
        /// </summary>
        /// <param name="fact"></param>
        /// <returns></returns>
        public Task<bool> Exists(string fact);
    }
}
