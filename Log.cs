using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TwitchChirperChat.TwitchIrc;

namespace TwitchChirperChat
{
    public static class Log
    {
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
