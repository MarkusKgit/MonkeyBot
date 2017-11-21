using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace MonkeyBot.Database.Entities
{
    public class GameServerEntity : BaseEntity
    {
        [Column("IP")]
        [Required]
        public string IPAsString { get; set; }

        [NotMapped]
        public IPEndPoint IP
        {
            get
            {
                if (string.IsNullOrEmpty(IPAsString))
                    return null;
                var splitIP = IPAsString.Split(':');
                var ip = IPAddress.Parse(splitIP[0]);
                var port = int.Parse(splitIP[1]);
                return new IPEndPoint(ip, port);
            }
            set
            {
                if (value == null)
                {
                    IPAsString = "";
                    return;
                }
                IPAsString = $"{value.Address}:{value.Port}";
            }
        }

        [Required]
        public long GuildId { get; set; }

        [Required]
        public long ChannelId { get; set; }

        public long? MessageId { get; set; }

        public string GameVersion { get; set; }

        public DateTime? LastVersionUpdate { get; set; }
    }
}