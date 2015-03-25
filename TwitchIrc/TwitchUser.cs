using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TwitchChirperChat.TwitchIrc
{
    public class TwitchUser
    {
        public string UserName { get; set; }
        public DateTime? SubscribeDateTime { get; set; }
        public int MonthsSubscribed { get; set; }

        public TwitchUser()
        {
            MonthsSubscribed = 1;
        }

        /// <returns>true if UserName is equal</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is TwitchUser))
                return false;

            return this.UserName.Equals(((TwitchUser)obj).UserName);
        }

        /// <returns>Hashcode of UserName, which should always be unique</returns>
        public override int GetHashCode()
        {
            return UserName.GetHashCode();
        }

        /// <returns>UserName</returns>
        public override string ToString()
        {
            return UserName;
        }
    }
}
