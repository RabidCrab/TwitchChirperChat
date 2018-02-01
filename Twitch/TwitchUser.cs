using System;

namespace Twitch
{
    /// <summary>
    /// A generic Twitch User with no interal data parsing or logic beyond the object matching overrides
    /// </summary>
    public class User
    {
        public string UserName { get; set; }
        public DateTime? SubscribeDateTime { get; set; }
        public int MonthsSubscribed { get; set; }
        public DateTime? FollowedDateTime { get; set; }

        public User()
        {
            MonthsSubscribed = 1;
        }

        /// <returns>true if UserName is equal</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is User))
                return false;

            return this.UserName.Equals(((User)obj).UserName);
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
