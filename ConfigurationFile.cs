using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

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
    }
}
