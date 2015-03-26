using System;
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
            if (_adminIrc == null) return;
            if (!_adminIrc.IsConnected) return;

            _adminIrc.SendMessage(ex.GetType() + " - " + ex.Message);

            if (ex.InnerException != null)
                _adminIrc.SendMessage(ex.InnerException.GetType() + " - " + ex.InnerException.Message);
        }
    }
}
