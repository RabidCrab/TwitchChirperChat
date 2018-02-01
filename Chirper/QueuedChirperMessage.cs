using System;

namespace TwitchChirperChat
{
    /// <summary>
    /// Used exclusively for a message that gets put into the queue. This is used specifically to hold
    /// the time it was added to the queue and the citizenId. I don't want to have to get the citizen's Id
    /// if I don't send the message, so I use this until I actually want to send the message
    /// </summary>
    public class TwitchIrcMessage
    {
        private uint _citizenId = 0u;
        public uint CitizenId
        {
            get { return _citizenId; }
            set
            {
                _citizenId = value;
                IsCitizenIdSet = true; 
            }
        }
        public string CitizenName { get; private set; }
        public bool IsCitizenIdSet { get; private set; }
        public string Message { get; set; }
        public DateTime QueueTime { get; private set; }
        public bool IsIrcUserMentioned { get; private set; }

        public TwitchIrcMessage(string citizenName, string message, bool isIrcUserMentioned = false)
        {
            IsCitizenIdSet = false;
            CitizenName = citizenName;
            Message = message;
            QueueTime = DateTime.Now;
            IsIrcUserMentioned = isIrcUserMentioned;
        }

        public TwitchIrcMessage(string citizenName, string message, uint citizenId, bool isIrcUserMentioned = false)
        {
            _citizenId = citizenId;
            IsCitizenIdSet = true;
            CitizenName = citizenName;
            Message = message;
            QueueTime = DateTime.Now;
            IsIrcUserMentioned = isIrcUserMentioned;
        }
    }
}
