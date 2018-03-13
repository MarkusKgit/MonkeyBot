using dokas.FluentStrings;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MonkeyBot.Database.Entities
{
    public class GuildConfigEntity : BaseEntity
    {
        [Required]
        [Column]
        public long GuildId { get; set; }

        [Required]
        public string CommandPrefix { get; set; }

        public string WelcomeMessageText { get; set; }

        [Required]
        public bool ListenToFeeds { get; set; }

        [Column("Rules")]
        public string RulesAsString { get; set; }

        [NotMapped]
        public List<string> Rules
        {
            get { return RulesAsString.IsEmpty() ? null : RulesAsString.Split(';').ToList(); }
            set
            {
                RulesAsString = (value == null || value.Count < 1) ? "" : string.Join(";", value);
            }
        }

        [Column("FeedUrls")]
        public string FeedUrlsAsString { get; set; }

        [NotMapped]
        public List<string> FeedUrls
        {
            get { return FeedUrlsAsString.IsEmpty() ? null : FeedUrlsAsString.Split(';').ToList(); }
            set
            {
                FeedUrlsAsString = (value == null || value.Count < 1) ? "" : string.Join(";", value);
            }
        }

        public long FeedChannelId { get; set; }
    }
}