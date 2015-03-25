using System.Runtime.Remoting.Messaging;
using ICities;

namespace TwitchChirperChat
{
    public class ModInfo : IUserMod
    {
        public string Name
        {
            get { return "Twitch Chirper Chat"; }
        }
        public string Description
        {
            get { return "Turn the Chirper into a Twitch chat feed"; }
        }
    }
}
