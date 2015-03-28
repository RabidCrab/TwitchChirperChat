using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using TwitchChirperChat.Twitch.TwitchApi;
using TwitchChirperChat.Twitch.TwitchIrc;
using UnityEngine;
using Timer = System.Timers.Timer;

namespace TwitchChirperChat
{
    public class ChirperExtension : ChirperExtensionBase
    {
        /// <summary>
        /// The Twitch Irc client instance we use to read/write information to Irc
        /// </summary>
        internal static TwitchIrcClient IrcClient = new TwitchIrcClient();

        /// <summary>
        /// The API manages existing subscribers, and new/existing followers
        /// </summary>
        private TwitchApiManager _apiManager;

        /// <summary>
        /// The message timer
        /// </summary>
        private static Timer _messageTimer;

        /// <summary>
        /// List of messages to pass
        /// </summary>
        private static List<KeyValuePair<MessagePriority, QueuedChirperMessage>> _messageQueue = new List<KeyValuePair<MessagePriority, QueuedChirperMessage>>();

        /// <summary>
        /// Used for deletion of default messages
        /// </summary>
        private AudioClip _messageSound = null;
        private static MessageManager _messageManager;
        private CitizenMessage _messageRemovalTarget = null;

        /// <summary>
        /// We don't want to queue up a crapton of messages while the game is paused and stuff like that
        /// </summary>
        private static bool IsPaused
        {
            get { return SimulationManager.instance.SimulationPaused; }
        }

        /// <summary>
        /// On creation, startup the IRC client to hook onto the twitch chat
        /// </summary>
        public override void OnCreated(IChirper c)
        {
            _messageManager = GameObject.Find("MessageManager").GetComponent<MessageManager>();
            Configuration.ReloadConfigFile();

            if (IrcClient == null)
                IrcClient = new TwitchIrcClient();

            // The noise will drive people bonkers
            ChirpPanel cp = ChirpPanel.instance;
            if (cp != null)
                cp.m_NotificationSound = null;

            // If they're using the default username, make them aware of the options tab
            AddMessage("chirpertestclient", "Welcome to Twitch Chirper Chat! Click the Options button above to do some simple setup to get started!", true);

            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message,
                "User Name: " + Configuration.ConfigurationSettings.UserName);
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message,
                "Channel: " + Configuration.ConfigurationSettings.IrcChannel);
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Connecting to Irc...");

            try
            {
                if (_apiManager != null)
                    _apiManager.Dispose();
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch {}

            _apiManager = new TwitchApiManager(Configuration.ConfigurationSettings.IrcChannel);
            _apiManager.NewFollowers += _apiManager_NewFollowers;
            _apiManager.StartWatching();

            // Hook up the Irc client events and execute a connection
            IrcClient.ChatMessageReceived += _ircClient_ChatMessageReceived;
            IrcClient.Disconnected += _ircClient_Disconnected;
            IrcClient.Connected += _ircClient_Connected;
            IrcClient.NewSubscriber += _ircClient_NewSubscriber;
            IrcClient.Connect(Configuration.ConfigurationSettings.UserName, Configuration.ConfigurationSettings.OAuthKey, Configuration.ConfigurationSettings.IrcChannel);

            if (_messageTimer != null)
            {
                // If the timer isn't null we're going to attempt to stop it then set it to null
                // ReSharper disable once EmptyGeneralCatchClause
                try { _messageTimer.Close(); } catch { }

                _messageTimer = null;
            }

            _messageTimer = new Timer(Configuration.ConfigurationSettings.DelayBetweenChirperMessages) {AutoReset = true};
            _messageTimer.Elapsed += _messageTimer_Elapsed;
            _messageTimer.Start();

            // Try and get the message sound we use. Not absolutely necessary
            /*try
            {
                _messageSound = Singleton<ChirpPanel>.instance.m_NotificationSound;
            }
            catch (Exception ex)
            {
                Log.AddEntry(ex);
            }*/
        }

        public static void ChangeTimerDelay(int delay)
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

            _messageTimer = new Timer(delay) { AutoReset = true };
            _messageTimer.Elapsed += _messageTimer_Elapsed;
            _messageTimer.Start();
        }

        void _ircClient_Connected(object source, ConnectedEventArgs e)
        {
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Irc connected");
        }

        /// <summary>
        /// On timer we're going to check the message queue and post one of the messages
        /// </summary>
        private static void _messageTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (IsPaused)
                return;

            try
            {
                SetPrivateVariable<bool>(MessageManager.instance, "m_canShowMessage", true);

                if (_messageQueue.Count == 0)
                    return;

                // First order by priority. Then by @Name mentions UNLESS they've disabled it, then by send date 
                KeyValuePair<MessagePriority, QueuedChirperMessage> messagePair = _messageQueue.OrderBy(x => x.Key).ThenByDescending(x => x.Value.IsIrcUserMentioned || !Configuration.ConfigurationSettings.PrioritizePersonallyAddressedMessages).ThenBy(x => x.Value.QueueTime).FirstOrDefault();

                if (messagePair.Equals(default(KeyValuePair<MessagePriority, QueuedChirperMessage>))) return;

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
                    messagePair.Value.CitizenId = LookupCitizenId(messagePair.Value.CitizenName);

                if (messagePair.Value.CitizenId != 0u)
                {
                    // We don't need to stinkin' countdown timer. I want to use my own, but I'm going to leave the delta timer alone
                    // just in case someone wants to post their own messages and expect a reasonable timer countdown
                    SetPrivateVariable<bool>(MessageManager.instance, "m_canShowMessage", true);
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
                Log.AddEntry(ex);
            }
        }

        void _ircClient_Disconnected(object source, DisconnectedEventArgs e)
        {
            // If it failed from an exception, clean everything up and notify the user the mod is shutting down
            if (e.Message != null)
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Error! Client was disconnected from Irc for some reason!");

                //OnReleased();
            }
        }

        /// <summary>
        /// Got a new/repeat sub!
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        void _ircClient_NewSubscriber(object source, NewSubscriberEventArgs e)
        {
            // Make sure the user wants to see new/repeat subs
            if (!Configuration.ConfigurationSettings.ShowSubscriberMessages)
                return;

            if (e.User.MonthsSubscribed < 2)
            {
                AddMessage(e.User.UserName, Configuration.ConfigurationSettings.NewSubscriberMessage, false, true);
                return;
            }

            if (e.User.MonthsSubscribed < 6)
            {
                AddMessage(e.User.UserName,
                    String.Format(Configuration.ConfigurationSettings.RepeatSubscriberMessage, e.User.MonthsSubscribed.ToString()), false, true);
                return;
            }

            if (e.User.MonthsSubscribed >= 6)
            {
                AddMessage(e.User.UserName, String.Format(Configuration.ConfigurationSettings.SeniorSubscriberMessage, e.User.MonthsSubscribed.ToString()), false, true);
                return;
            }
        }

        void _apiManager_NewFollowers(object source, NewFollowersEventArgs e)
        {
            if (!Configuration.ConfigurationSettings.ShowNewFollowersMessage)
                return;
            
            //DebugOutputPanel.AddMessage(PluginManager.MessageType. Message,Configuration.ConfigurationSettings.IrcChannel + " - " + String.Format(Configuration.ConfigurationSettings.NewFollowersMessage, String.Join(", ", e.Users.Select(x => x.UserName).ToArray())));

            AddMessage(Configuration.ConfigurationSettings.IrcChannel,
                String.Format(Configuration.ConfigurationSettings.NewFollowersMessage, String.Join(", ", e.Users.Select(x => x.UserName).ToArray())),
                true,
                true,
                false);
        }

        /// <summary>
        /// Executed on each chat message received and parsed by the Irc client
        /// </summary>
        /// <param name="source">The IrcClient instance the call was made from</param>
        /// <param name="e"></param>
        void _ircClient_ChatMessageReceived(object source, ChatMessageReceivedEventArgs e)
        {
            // I put up general errors and whatnot in Chirper since my mod specifically modifies it
            if (e.Message.IsUserFeedback)
            {
                SetPrivateVariable<bool>(MessageManager.instance, "m_canShowMessage", true);
                MessageManager.instance.QueueMessage(new Message(e.Message.User.UserName,
                    e.Message.Message, "", LookupCitizenId(e.Message.User.UserName)));
            }
            else
            {
                AddMessage(e.Message.User.UserName, e.Message.Message, false, e.Message.IsIrcUserMentioned);
            }
        }

        /// <summary>
        /// Add a message to chirper queue
        /// </summary>
        private void AddMessage(QueuedChirperMessage message, bool isCritical = false, bool isNewSubscriber = false, bool isIrcUserMentioned = false)
        {
            var priority = MessagePriority.GeneralChat;
            var username = message.CitizenName.ToLowerInvariant();

            // A reserved hashtag just for the main dev + future contributors
            if (username != "rabidcrabgt" && Regex.IsMatch(Regex.Escape(message.Message), "#moddev", RegexOptions.IgnoreCase))
                message.Message = Regex.Replace(message.Message, "#moddev", "", RegexOptions.IgnoreCase);

            // Time to figure out the message priority
            if (username == "rabidcrabgt" && message.Message.Contains("#ModDev"))
            {
                // I don't want all of my chats to be priority, but if I get a shout-out by a streamer I'd like to be able to say hi to everyone
                // and have it at least look kinda cool with a snazzy hashtag.
                // I'm so lonely
                priority = MessagePriority.ModUser;
                isCritical = true;
            }
            else if (isCritical)
                priority = MessagePriority.Critical;
            else if (username == IrcClient.UserName.ToLowerInvariant())
            {
                priority = MessagePriority.ModUser;
                isCritical = true;
            }
            else if (isNewSubscriber)
                priority = MessagePriority.NewSubscriber;
            else if (IrcClient.Moderators.ContainsKey(username))
                priority = MessagePriority.Moderator;
            else if (IrcClient.Subscribers.ContainsKey(username))
                priority = MessagePriority.Subscriber;

            if (IsPaused && !isCritical)
                return;

            _messageQueue.Add(new KeyValuePair<MessagePriority, QueuedChirperMessage>(priority, message));

        }

        /// <summary>
        /// Add a message to chirper. It's obsolete to use, but I'll need to do some serious refactoring to make it more reasonable
        /// </summary>
        private void AddMessage(string citizenName, string text, bool isCritical = false, bool isNewSubscriber = false, bool isIrcUserMentioned = false)
        {
            if (IsPaused && !isCritical)
                return;

            if (text.Length > Configuration.ConfigurationSettings.MaximumMessageSize)
                text = text.Substring(0, Configuration.ConfigurationSettings.MaximumMessageSize);

            // The citizen ID will not be pulled until the message makes it through the queue
            AddMessage(new QueuedChirperMessage(citizenName, text, isIrcUserMentioned), isCritical, isNewSubscriber, isIrcUserMentioned);
        }

        /// <summary>
        ///  Clean up the leftovers
        /// </summary>
        public override void OnReleased()
        {
            try
            {
                // Make sure we reset the notification sound back to normal, or we might really screw something up
                ChirpPanel cp = ChirpPanel.instance;
                if (cp != null)
                    cp.m_NotificationSound = _messageSound;

                // Make sure the Irc client is really gone so it releases the Irc port
                //if (_adminClient != null)
                //    _adminClient.Dispose();

                //_adminClient = null;

                // Make sure the Irc client is really gone so it releases the Irc port
                if (IrcClient != null)
                    IrcClient.Dispose();

                IrcClient = null;
            }
            catch (Exception ex)
            {
                Log.AddEntry(ex);
            }

            try
            {
                _messageTimer.Elapsed -= _messageTimer_Elapsed;
                _messageTimer.Close();
                _messageTimer = null;
            }
            // We don't care about the timer
            // ReSharper disable once EmptyGeneralCatchClause
            catch { }
        }

        /// <summary>
        /// OnNewMessage runs before the message can be found in the stack. So we try to get
        /// rid of it here Mostly copied from https://github.com/mabako/reddit-for-city-skylines/
        /// </summary>
        public override void OnUpdate()
        {
            // Detect keypress
            if (Event.current.alt && Input.GetKeyDown(KeyCode.C))
            {
                // Toggle options

            }

            // If there's no message to remove, continue on
            if (_messageRemovalTarget == null)
                return;

            // This code is roughly based on the work by Juuso "Zuppi" Hietala.
            // Get the Chirper container, where all of the chirps reside
            var container = ChirpPanel.instance.transform.FindChild("Chirps").FindChild("Clipper").FindChild("Container").gameObject.transform;
            for (var i = 0; i < container.childCount; ++i)
            {
                // Keep looping until we get the one we want. It should pretty much be the very first one we snag every time, on very rare
                // occurence the second one
                if (!container.GetChild(i).GetComponentInChildren<UILabel>().text.Equals(_messageRemovalTarget.GetText()))
                    continue;

                // Reset the notification sound back to normal
                //ChirpPanel.instance.m_NotificationSound = _messageSound;

                // Remove the message
                UITemplateManager.RemoveInstance("ChirpTemplate", container.GetChild(i).GetComponent<UIPanel>());
                MessageManager.instance.DeleteMessage(_messageRemovalTarget);

                // Collapses the Chirper panel back
                //ChirpPanel.instance.Collapse();

                // We're done here
                _messageRemovalTarget = null;

                break;
            }
        }

        /// <summary>
        /// On a new message event we check and see if the message is one we want to keep or delete. Mostly copied from https://github.com/mabako/reddit-for-city-skylines/
        /// </summary>
        public override void OnNewMessage(IChirperMessage message)
        {
            // If the message should be filtered, target it for removal
            var citizenMessage = message as CitizenMessage;
            if (citizenMessage == null) return;

            if (!Configuration.ConfigurationSettings.ShowDefaultChirperMessages)
                _messageRemovalTarget = citizenMessage;
        }

        /// <summary>
        /// Get the Citizen or rename one and return it. This method really shouldn't be here, but I haven't had the opportunity to move it yet. Mostly copied from
        /// https://github.com/mabako/reddit-for-city-skylines/
        /// </summary>
        /// <param name="name">The name of the Citizen to return</param>
        /// <returns>A citizen with the name to create a Chirper method from</returns>
        private static uint LookupCitizenId(string name)
        {
            if (string.IsNullOrEmpty(name)) return 0;

            // For debugging purposes
            //return MessageManager.instance.GetRandomResidentID();

            // Overwrite any CIM's name by their reddit username.
            // To be fair: this was the more interesting part.
            try
            {
                // Get the shared lock to guarantee we don't interrupt the game attempting to read the info we need
                var threadLock = GetPrivateVariable<object>(InstanceManager.instance, "m_lock");
                // Make sure we're locked
                do { } while (!Monitor.TryEnter(threadLock, SimulationManager.SYNCHRONIZE_TIMEOUT));

                // Lock initiated, pull the data and release the lock
                try
                {
                    // Pull the citizens, then immediately get the first one with a matching name
                    var citizen = GetPrivateVariable<Dictionary<InstanceID, string>>(InstanceManager.instance, "m_names").FirstOrDefault(x => x.Value == name);

                    // It's the equivalent of a null check for the FirstOrDefault call. If there
                    // was a name return it
                    if (!default(KeyValuePair<InstanceID, string>).Equals(citizen))
                        return citizen.Key.Citizen;
                }
                finally
                {
                    Monitor.Exit(threadLock);
                }

                for (int i = 0; i < 500; ++i)
                {
                    // Attempt to pull a random resident
                    var id = MessageManager.instance.GetRandomResidentID();

                    // If we have no residents we can't post to Chirper
                    if (id == 0u)
                        break;

                    // Citizen doesn't exist anymore. Could have died
                    if (CitizenManager.instance.m_citizens.m_buffer[id].m_flags == Citizen.Flags.None)
                        continue;

                    // Citizen already has a custom name, keep going
                    if ((CitizenManager.instance.m_citizens.m_buffer[id].m_flags & Citizen.Flags.CustomName) != Citizen.Flags.None)
                        continue;

                    // Citizen is clean of modifications, use this one
                    CitizenManager.instance.StartCoroutine(CitizenManager.instance.SetCitizenName(id, name));

                    return id;
                }

                // Either we have nobody, or all the names are taken. If it's the latter they're shit out of luck until the next random
                // call string. Eventually this won't be needed because I'll be renaming citizens as users join chat and their citizen will
                // be automatically added
            }
            catch
            {
                // not sure if this would happen often. Who knows.
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, string.Format("[TwitchChriperChat] Failed to pick random citizen name for {0}", name));
            }

            // Either we have no people, or we have some people but couldn't find anyone to use for our purposes,
            // or we don't want people renamed.
            return 0;
        }

        /// <summary>
        /// Resolve private assembly fields. Copied from https://github.com/mabako/reddit-for-city-skylines/
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        protected static T GetPrivateVariable<T>(object obj, string fieldName)
        {
            var fieldInfo = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (fieldInfo != null)
                return (T)fieldInfo.GetValue(obj);
            else
                throw new ArgumentNullException("(" + fieldName + ") is null!");
        }

        /// <summary>
        /// Resolve private assembly fields. Copied from https://github.com/mabako/reddit-for-city-skylines/
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        protected static void SetPrivateVariable<T>(object obj, string fieldName, T val)
        {
            var fieldInfo = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            fieldInfo.SetValue(obj, val);
        }
    }

    public enum MessagePriority
    {
        Critical = 1, // Program exceptions will be posted via here
        ModUser = 2, // The user themselves
        NewSubscriber = 3, // Yay, new/repeat subscribers!
        Moderator = 4, 
        Subscriber = 5,
        GeneralChat = 6, // General chat is cleared out after each chat grab
    }
}
