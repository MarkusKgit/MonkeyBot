using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public interface IPictureUploadService
    {
        /// <summary>
        /// Upload a picture from a filepath on disc and retrieve the image url on succcess
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>Image Url</returns>
        Task<string> UploadPictureAsync(string filePath, string id);
    }
}