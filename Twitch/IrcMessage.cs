using System.Text.RegularExpressions;

namespace Twitch
{
    /// <summary>
    /// Contains all paramaters for a Twitch Irc message
    /// </summary>
    public class IrcMessage
    {
        public User User { get; private set; }
        public string IrcChannel { get; private set; }
        public string IrcUserName { get; private set; }
        public string Message { get; private set; }
        public bool IsUserFeedback { get; private set; }

        /// <summary>
        /// Returns true if the IrcUserName is found in Message
        /// </summary>
        public virtual bool IsIrcUserMentioned
        {
            get { return Regex.IsMatch(Message, "@" + IrcUserName, RegexOptions.IgnoreCase); }
        }

        public IrcMessage(User user, string ircUserName, string ircChannel, string message, bool isError = false)
        {
            User = user;
            IrcChannel = ircChannel;
            Message = message;
            IrcUserName = ircUserName;
            IsUserFeedback = isError;
        }
    }
}
