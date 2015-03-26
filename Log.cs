using System;
using ColossalFramework.Plugins;
using TwitchChirperChat.Twitch.TwitchIrc;
using TwitchChirperChat.TwitchIrc;

namespace TwitchChirperChat
{
    /// <summary>
    /// All of the logging logic for the program
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// This is a temporary solution. I'll be setting up a database and sending the exceptions through there eventually
        /// </summary>
        private static TwitchIrcClient _adminIrc = null;

        public static void SetIrcClient(TwitchIrcClient adminIrc)
        {
            _adminIrc = adminIrc;
        }

        public static void AddEntry(Exception ex)
        {
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, ex.GetType() + " - " + ex.Message + " - " + ex.TargetSite + " - " + ex.StackTrace);

            if (ex.InnerException != null)
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, ex.InnerException.GetType() + " - " + ex.InnerException.Message + " - " + ex.InnerException.TargetSite + " - " + ex.InnerException.StackTrace);
        }
    }
}
