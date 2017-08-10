using System;

namespace MonkeyBot.Services.Common
{
    public class FeedItem
    {
        public string Link { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime PublishDate { get; set; }
    }
}
