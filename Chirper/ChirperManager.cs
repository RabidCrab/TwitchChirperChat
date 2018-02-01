using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Timers;
using TwitchChirperChat.Twitch.Helpers;
using Timer = System.Timers.Timer;
using Twitch;

namespace TwitchChirperChat.Chirper
{
    public class ChirperManager : IChirperManager
    {
        private static IIrcClient _ircClient;
        private static ILog _log;

        private static Timer _messageTimer;

        private static List<KeyValuePair<MessagePriority, TwitchIrcMessage>> _messageQueue = new List<KeyValuePair<MessagePriority, TwitchIrcMessage>>();

        /// <summary>
        /// We don't want to queue up a crapton of messages while the game is paused and stuff like that. While paused the message queue will not be added to
        /// unless the message is critical
        /// </summary>
        private static bool IsPaused
        {
            get { return SimulationManager.instance.SimulationPaused; }
        }

        public ChirperManager(IIrcClient ircClient, ILog log)
        {
            _ircClient = ircClient;
            _log = log;

            if (_messageTimer != null)
            {
                // If the timer isn't null we're going to attempt to stop it then set it to null
                // ReSharper disable once EmptyGeneralCatchClause
                try { _messageTimer.Close(); } catch { }

                _messageTimer = null;
            }

            _messageTimer = new Timer(Configuration.ConfigurationSettings.DelayBetweenChirperMessages) { AutoReset = true };
            _messageTimer.Elapsed += _messageTimer_Elapsed;
            _messageTimer.Start();
        }

        /// <summary>
        /// Change the timer delay between messages
        /// </summary>
        /// <param name="delay">The time in milliseconds between each message. Shortest is 100</param>
        public void ChangeTimerDelay(int delay)
        {
            if (_messageTimer != null)
            {
                _messageTimer.Elapsed -= _messageTimer_Elapsed;
                // If the timer isn't null we're going to attempt to stop it then set it to null
                // ReSharper disable once EmptyGeneralCatchClause
                try { _messageTimer.Close(); }
                catch { }

                _messageTimer = null;
            }

            _messageTimer = new Timer(delay < 100 ? 100 : delay) { AutoReset = true };
            _messageTimer.Elapsed += _messageTimer_Elapsed;
            _messageTimer.Start();
        }

        /// <summary>
        /// On timer we're going to check the message queue and post one of the messages
        /// </summary>
        private void _messageTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (IsPaused)
                return;

            try
            {
                ReflectionHelper.SetPrivateVariable<bool>(MessageManager.instance, "m_canShowMessage", true);

                if (_messageQueue.Count == 0)
                    return;

                // First order by priority. Then by @Name mentions UNLESS they've disabled it, then by send date 
                KeyValuePair<MessagePriority, TwitchIrcMessage> messagePair = _messageQueue.OrderBy(x => x.Key).ThenByDescending(x => x.Value.IsIrcUserMentioned || !Configuration.ConfigurationSettings.PrioritizePersonallyAddressedMessages).ThenBy(x => x.Value.QueueTime).FirstOrDefault();

                if (messagePair.Equals(default(KeyValuePair<MessagePriority, TwitchIrcMessage>))) return;

                // This is where we check for filters. If the person doesn't want to see certain messages, cut them off here
                if (!Configuration.ConfigurationSettings.ShowGeneralChatMessages && messagePair.Key == MessagePriority.GeneralChat)
                {
                    _messageQueue.Remove(messagePair);
                    if (_messageQueue.Count > 0)
                        _messageTimer_Elapsed(null, null);
                    return;
                }
                if (!Configuration.ConfigurationSettings.ShowSubscriberChatMessages && messagePair.Key == MessagePriority.Subscriber)
                {
                    _messageQueue.Remove(messagePair);
                    if (_messageQueue.Count > 0)
                        _messageTimer_Elapsed(null, null);
                    return;
                }
                if (!Configuration.ConfigurationSettings.ShowModeratorChatMessages && messagePair.Key == MessagePriority.Moderator)
                {
                    _messageQueue.Remove(messagePair);
                    if (_messageQueue.Count > 0)
                        _messageTimer_Elapsed(null, null);
                    return;
                }

                // If it doesn't have a CitizenId, get one
                if (!messagePair.Value.IsCitizenIdSet)
                    messagePair.Value.CitizenId = ChirperExtension.GetCitizenId(messagePair.Value.CitizenName);

                if (messagePair.Value.CitizenId != 0u)
                {
                    // We don't need to stinkin' countdown timer. I want to use my own, but I'm going to leave the delta timer alone
                    // just in case someone wants to post their own messages and expect a reasonable timer countdown
                    ReflectionHelper.SetPrivateVariable<bool>(MessageManager.instance, "m_canShowMessage", true);
                    MessageManager.instance.QueueMessage(new Message(messagePair.Value.CitizenName,
                        messagePair.Value.Message, "", messagePair.Value.CitizenId));
                }

                _messageQueue.Remove(messagePair);

                // Clear out general chat if there's too many messages
                if (_messageQueue.Count > Configuration.ConfigurationSettings.MaximumGeneralChatMessageQueue)
                    _messageQueue.RemoveAll(x => x.Key == MessagePriority.GeneralChat);

                // Still too many messages, get rid of sub chat
                if (_messageQueue.Count > Configuration.ConfigurationSettings.MaximumSubscriberChatMessageQueue)
                    _messageQueue.RemoveAll(x => x.Key == MessagePriority.Subscriber);

                // Still too many messages, get rid of mod chat
                if (_messageQueue.Count > Configuration.ConfigurationSettings.MaximumModeratorChatMessageQueue)
                    _messageQueue.RemoveAll(x => x.Key == MessagePriority.Moderator);
            }
            catch (Exception ex)
            {
                _log.AddEntry(ex);
            }
        }

        /// <summary>
        /// Add a message to chirper. It's obsolete, but I still need to do some serious refactoring as I build a custom UI for Chirper
        /// </summary>
        public void AddMessage(string citizenName, string message, MessagePriority priority = MessagePriority.None, bool isIrcUserMentioned = false)
        {
            if (IsPaused 
                && priority != MessagePriority.Critical
                && priority != MessagePriority.ModUser)
                return;

            if (message.Length > Configuration.ConfigurationSettings.MaximumMessageSize)
                message = message.Substring(0, Configuration.ConfigurationSettings.MaximumMessageSize);

            TwitchIrcMessage ircMessage = new TwitchIrcMessage(citizenName.ToLowerInvariant(), message, isIrcUserMentioned);

            // A reserved hashtag for the contributors
            if (ircMessage.CitizenName.ToLowerInvariant() != "rabidcrabgt" && Regex.IsMatch(Regex.Escape(ircMessage.Message), "#moddev", RegexOptions.IgnoreCase))
                ircMessage.Message = Regex.Replace(ircMessage.Message, "#moddev", "", RegexOptions.IgnoreCase);

            if (priority == MessagePriority.None)
            {
                // Time to figure out the message priority
                if (ircMessage.CitizenName.ToLowerInvariant() == "rabidcrabgt" && ircMessage.Message.ToLowerInvariant().Contains("#ModDev"))
                {
                    // I don't want all of my chats to be priority, but if I get a shout-out by a streamer I'd like to be able to say hi to everyone
                    // and have it at least look kinda cool with a snazzy hashtag.
                    priority = MessagePriority.ModUser;
                }
                else if (ircMessage.CitizenName == _ircClient.UserName.ToLowerInvariant())
                {
                    priority = MessagePriority.ModUser;
                }
                else if (_ircClient.Moderators.ContainsKey(ircMessage.CitizenName))
                    priority = MessagePriority.Moderator;
                else if (_ircClient.Subscribers.ContainsKey(ircMessage.CitizenName))
                    priority = MessagePriority.Subscriber;
            }

            _messageQueue.Add(new KeyValuePair<MessagePriority, TwitchIrcMessage>(priority, ircMessage));
        }

        public void Dispose()
        {
            try
            {
                _messageTimer.Close();
                _messageTimer.Dispose();
            }
            // No need to worry about the timer not dying as intended
            catch { }
        }
    }
}
