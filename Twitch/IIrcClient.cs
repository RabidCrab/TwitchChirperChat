using System.Collections.Generic;

namespace Twitch
{
    public interface IIrcClient
    {
        bool IsConnected { get; }
        Dictionary<string, User> LoggedInUsers { get; }
        Dictionary<string, User> Moderators { get; }
        Dictionary<string, User> Subscribers { get; }
        string UserName { get; }

        event ChatMessageReceivedHandler ChatMessageReceived;
        event ConnectedHandler Connected;
        event DisconnectedHandler Disconnected;
        event NewSubscriberHandler NewSubscriber;

        void Connect(string userName, string password, string channel);
        void Dispose();
        void Reconnect(string userName, string oAuthToken, string channel);
        void SendMessage(string message);
    }
}