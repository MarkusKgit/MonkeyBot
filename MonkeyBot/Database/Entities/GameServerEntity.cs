using dokas.FluentStrings;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace MonkeyBot.Database.Entities
{
    public class GameServerEntity : BaseGuildEntity
    {
        [Column("IP")]
        [Required]
        public string IPAsString { get; set; }

        [NotMapped]
        public IPEndPoint IP
        {
            get
            {
                if (IPAsString.IsEmpty())
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
        public ulong ChannelId { get; set; }

        public ulong? MessageId { get; set; }

        public string GameVersion { get; set; }

        public DateTime? LastVersionUpdate { get; set; }
    }
}