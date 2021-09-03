using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface ICatService
    {
        /// <summary>
        /// Get a random cat picture of the given <paramref name="breed"/> or from all cats if <paramref name="breed"/> is left empty
        /// </summary>
        /// <param name="breed">The name of the breed to get a picture for</param>
        Task<Uri> GetRandomPictureUrlAsync(string breed = "");

        /// <summary>
        /// Get a all cat breeds
        /// </summary>
        Task<List<string>> GetBreedsAsync();
    }
}
