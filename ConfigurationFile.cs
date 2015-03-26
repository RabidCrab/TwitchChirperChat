namespace TwitchChirperChat
{
    /// <summary>
    /// The actual file layout that's parsed into an XML document
    /// </summary>
    public class ConfigurationFile
    {
        public string UserName { get; set; }
        public string OAuthKey { get; set; }
        public string IrcChannel { get; set; }

        /// <summary>
        /// Default 9
        /// </summary>
        public int DelayBetweenChirperMessages { get; set; }

        /// <summary>
        /// Default true. If someone does @YourName, they will get chat priority
        /// </summary>
        public bool PrioritizePersonallyAddressedMessages { get; set; }

        public string NewSubscriberMessage { get; set; }
        public string RepeatSubscriberMessage { get; set; }
        public string SeniorSubscriberMessage { get; set; }
        public bool ShowSubscriberMessages { get; set; }

        public int MaximumGeneralChatMessageQueue { get; set; }
        public int MaximumSubscriberChatMessageQueue { get; set; }
        public int MaximumModeratorChatMessageQueue { get; set; }

        public bool RenameCitizensToLoggedInUsers { get; set; }
        public bool RenameCitizensToFollowers { get; set; }

        public bool ShowGeneralChatMessages { get; set; }
        public bool ShowSubscriberChatMessages { get; set; }
        public bool ShowModeratorChatMessages { get; set; }

        public string NewFollowersMessage { get; set; }
        public bool ShowNewFollowersMessage { get; set; }

        public bool ShowDefaultChirperMessages { get; set; }
        public int MaximumMessageSize { get; set; }
    }
}
