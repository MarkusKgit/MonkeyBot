namespace MonkeyBot.Common
{
    public class CloudinaryCredentials
    {
        public string Cloud { get; }
        public string ApiKey { get; }
        public string ApiSecret { get; }

        public CloudinaryCredentials(string cloud, string apiKey, string apiSecret)
        {
            Cloud = cloud;
            ApiKey = apiKey;
            ApiSecret = apiSecret;
        }
    }
}