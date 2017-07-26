using System;
using System.Collections.Generic;
using System.Text;

namespace MonkeyBot.Common
{
    public class CombinedID
    {
        public ulong? GuildID { get; set; }
        public ulong? ChannelID { get; set; }
        public ulong? UserID { get; set; }

        public CombinedID()
        {
        }

        public CombinedID(ulong? guildID, ulong? channelID, ulong? userID)
        {
            GuildID = guildID;
            ChannelID = channelID;
            UserID = userID;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is CombinedID))
                return false;
            CombinedID objID = obj as CombinedID;            
            //if both IDs have a value then the values must be equal or either one has no value (is null) then this id part can be ignored
            bool guild = ((this.GuildID.HasValue && objID.GuildID.HasValue && (this.GuildID == objID.GuildID)) || (!this.GuildID.HasValue || !objID.GuildID.HasValue));
            bool channel = ((this.ChannelID.HasValue && objID.ChannelID.HasValue && (this.ChannelID == objID.ChannelID)) || (!this.ChannelID.HasValue || !objID.ChannelID.HasValue));
            bool user = ((this.UserID.HasValue && objID.UserID.HasValue && (this.UserID == objID.UserID)) || (!this.UserID.HasValue || !objID.UserID.HasValue));
            return (guild && channel && user);    
        }

        public override int GetHashCode()
        {
            return (int)(GuildID?? 0) + (int)(ChannelID?? 0) + (int)(UserID?? 0);
        }
    }
}
