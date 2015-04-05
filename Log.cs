using System;
using ColossalFramework.Plugins;
using Twitch;

namespace TwitchChirperChat
{
    /// <summary>
    /// All of the logging logic for the program
    /// </summary>
    public class Log : ILog
    {
        public void AddEntry(Exception ex)
        {
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, ex.GetType() + " - " + ex.Message + " - " + ex.TargetSite + " - " + ex.StackTrace);

            if (ex.InnerException != null)
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, ex.InnerException.GetType() + " - " + ex.InnerException.Message + " - " + ex.InnerException.TargetSite + " - " + ex.InnerException.StackTrace);
        }
    }
}
