using ColossalFramework.IO;

namespace TwitchChirperChat
{
    /// <summary>
    /// A chirp that will eventually show up on Chirper. Copied the serialization logic from https://github.com/mabako/reddit-for-city-skylines/
    /// </summary>
    public class Message : MessageBase
    {
        private string m_author;
        private string m_text;
        private string m_keywords;
        private uint m_citizenId;

        /// <summary>
        /// Generate a Chirper message
        /// </summary>
        /// <param name="author">The cim name</param>
        /// <param name="text">The content of the message</param>
        /// <param name="keywords">Currently unused, but this was used to pass icon keywords. I'm keeping it because I have an idea for it in the near future</param>
        /// <param name="citizenId">The unique Id of the citizen</param>
        public Message(string author, string text, string keywords, uint citizenId)
        {
            m_author = author;
            m_keywords = keywords;
            m_text = text;
            m_citizenId = citizenId;

            HashtagKeywords();
        }

        /// <summary>
        /// Hashtag all of the icon keywords
        /// </summary>
        private void HashtagKeywords()
        {
            m_text = m_text.Replace("Kappa", "#Kappa");
            m_text = m_text.Replace("Kreygasm", "#Kreygasm");
        }

        public override uint GetSenderID()
        {
            return m_citizenId;
        }

        public override string GetSenderName()
        {
            return m_author;
        }

        public override string GetText()
        {
            return m_text;
        }

        /// <summary>
        /// We would want to ensure the same messages aren't shown twice, but if the user says the same thing twice,
        /// who are we to judge?
        /// </summary>
        /// <param name="other">The other message to compare against</param>
        /// <returns>true if they're similar</returns>
        public override bool IsSimilarMessage(MessageBase other)
        {
            return false;
        }

        public override void Serialize(DataSerializer s)
        {
            s.WriteSharedString(m_author);
            s.WriteSharedString(m_keywords);
            s.WriteSharedString(m_text);
            s.WriteUInt32(m_citizenId);
        }

        public override void Deserialize(DataSerializer s)
        {
            m_author = s.ReadSharedString();
            m_keywords = s.ReadSharedString();
            m_text = s.ReadSharedString();
            m_citizenId = s.ReadUInt32();
        }

        public override void AfterDeserialize(DataSerializer s)
        {
        }
    }
}
