using System;
using Twitch;

namespace TwitchChirperChat.Chirper
{
    internal interface IChirperManager : IDisposable
    {
        void AddMessage(string citizenName, string message, MessagePriority priority = MessagePriority.None, bool isIrcUserMentioned = false);

        void ChangeTimerDelay(int delay);
    }
}
