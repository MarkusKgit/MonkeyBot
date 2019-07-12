using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using MonkeyBot.Common;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class CloudinaryPictureUploadService : IPictureUploadService
    {
        private readonly Cloudinary cloudinary;

        public CloudinaryPictureUploadService()
        {
            var config = DiscordClientConfiguration.LoadAsync().GetAwaiter().GetResult();

            if (config.CloudinaryCredentials == null)
                return;
            Account account = new Account(
                config.CloudinaryCredentials.Cloud,
                config.CloudinaryCredentials.ApiKey,
                config.CloudinaryCredentials.ApiSecret);
            cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadPictureAsync(string filePath, string id)
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(filePath),
                PublicId = id
            };

            var uploadResult = await Task.Run(() => cloudinary?.Upload(uploadParams)).ConfigureAwait(false);

            return uploadResult?.SecureUri?.OriginalString ?? "";
        }
    }
}