using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IDogService
    {

        /// <summary>
        /// Get a random dog picture of the given <paramref name="breed"/> or from all dogs if <paramref name="breed"/> is left empty
        /// </summary>
        /// <param name="breed">The name of the breed to get a picture for</param>
        /// <returns></returns>
        Task<string> GetDogPictureUrlAsync(string breed = "");

        /// <summary>
        /// Get a all dog breeds
        /// </summary>
        /// <returns></returns>
        Task<List<string>> GetDogBreedsAsync();
    }
}