using ICities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using System.Xml.Serialization;
using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using TwitchChirperChat;
using TwitchChirperChat.TwitchIrc;
using UnityEngine;

namespace TwitchChirperChat
{
    public class ChirperExtension : ChirperExtensionBase
    {
        /// <summary>
        /// The Twitch Irc client instance we use to read/write information to Irc
        /// </summary>
        private TwitchIrcClient _ircClient = new TwitchIrcClient();

        public TwitchIrcClient AdminClient
        {
            get { return _adminClient; }
        }

        private TwitchIrcClient _adminClient = new TwitchIrcClient();

        private System.Timers.Timer _messageTimer;

        private List<KeyValuePair<MessagePriority, QueuedChirperMessage>> _messageQueue = new List<KeyValuePair<MessagePriority, QueuedChirperMessage>>();

        /// <summary>
        /// Both of these are used to manage the deletion of unwanted chirps. The _removalTargetCitizenMessage is the message
        /// tagged for deletion, and _messageSound is the default sound for chirp notifications. When a chirp comes in and it
        /// is getting tagged for deletion, we shut off the noise until the target chirp is deleted. The easiest way to do this is
        /// to set the audio clip to null so the game will not play it. After we delete the chirp, we restore the clip so the game
        /// can use it again
        /// </summary>
        private volatile CitizenMessage _removalTargetCitizenMessage = null;

        private AudioClip _messageSound = null;

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
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Connecting to Irc");

            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message,
                "User Name: " + Configuration.ConfigurationSettings.UserName);
            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message,
            //    "Auth Key: " + Configuration.ConfigurationSettings.OAuthKey);
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message,
                "Channel: " + Configuration.ConfigurationSettings.IrcChannel);

            // Hook up the Irc client events and execute a connection
            _ircClient.ChatMessageReceived += _ircClient_ChatMessageReceived;
            _ircClient.Disconnected += _ircClient_Disconnected;
            _ircClient.Connect(Configuration.ConfigurationSettings.UserName,
                Configuration.ConfigurationSettings.OAuthKey, Configuration.ConfigurationSettings.IrcChannel);
            _ircClient.NewSubscriber += _ircClient_NewSubscriber;

            // Any exceptions are going to get posted to this Irc. It won't help me with Irc issues, but I'll be dealing with that soon
            _adminClient.ChatMessageReceived += _ircClient_ChatMessageReceived;
            _adminClient.Connect("chirpertestclient", "oauth:eqtt3b1vl3dxmthyyzo9l5f2clyj5s", "chirpertestclient");

            if (_messageTimer != null)
            {
                // If the timer isn't null we're going to attempt to stop it then set it to null
                // ReSharper disable once EmptyGeneralCatchClause
                try
                {
                    _messageTimer.Close();
                }
                catch
                {
                }

                _messageTimer = null;
            }

            Log.SetIrcClient(AdminClient);

            _messageTimer = new System.Timers.Timer(9000) {AutoReset = true};
            _messageTimer.Elapsed += _messageTimer_Elapsed;
            _messageTimer.Start();

            // Try and get the message sound we use. Not absolutely necessary
            try
            {
                _messageSound = Singleton<ChirpPanel>.instance.m_NotificationSound;
            }
            catch (Exception ex)
            {
                Log.AddEntry(ex);
            }
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
                // Order it by priority, then by message date
                var messagePair = _messageQueue.OrderBy(x => x.Key).ThenBy(x => x.Value.QueueTime).FirstOrDefault();

                if (messagePair.Equals(default(KeyValuePair<MessagePriority, QueuedChirperMessage>))) return;

                // If it doesn't have a CitizenId, get one
                if (!messagePair.Value.IsCitizenIdSet)
                    messagePair.Value.CitizenId = LookupCitizenId(messagePair.Value.CitizenName);

                if (messagePair.Value.CitizenId != 0u)
                {

                    MessageManager.instance.QueueMessage(new Message(messagePair.Value.CitizenName,
                        messagePair.Value.Message, "", messagePair.Value.CitizenId));
                }

                _messageQueue.Remove(messagePair);
                // Clear out general chat
                _messageQueue.RemoveAll(x => x.Key == MessagePriority.GeneralChat);

                // Still too many messages, get rid of sub chat
                if (_messageQueue.Count > 20)
                    _messageQueue.RemoveAll(x => x.Key == MessagePriority.Subscriber);

                // Still too many messages, get rid of mod chat
                if (_messageQueue.Count > 10)
                    _messageQueue.RemoveAll(x => x.Key == MessagePriority.Moderator);
            }
            catch (Exception ex)
            {
                Log.AddEntry(ex);
            }
        }

        void _ircClient_Disconnected(object source, DisconnectedEventArgs e)
        {
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Disconnected from Irc");

            // If it failed from an exception, clean everything up and notify the user the mod is shutting down
            if (e.ClosedException != null)
            {
                lock (_removalTargetCitizenMessage)
                    _removalTargetCitizenMessage = null;

                OnReleased();
            }
        }

        /// <summary>
        /// Got a new/repeat sub!
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        void _ircClient_NewSubscriber(object source, NewSubscriberEventArgs e)
        {
            if (e.User.MonthsSubscribed < 2)
            {
                AddMessage(e.User.UserName, "Hey everyone, I just subscribed! #Newbie #WelcomeToTheParty", false, true);
                return;
            }

            if (e.User.MonthsSubscribed < 6)
            {
                AddMessage(e.User.UserName,
                    String.Format("I've just subscribed for {0} months in a row! #BestSupporterEver",
                        e.User.MonthsSubscribed.ToString()), false, true);
                return;
            }

            if (e.User.MonthsSubscribed >= 6)
                AddMessage(e.User.UserName, String.Format("I've been supporting the stream for {0} months in a row! #SeniorDiscount #GetOnMyLevel", e.User.MonthsSubscribed.ToString()), false, true);
        }

        /// <summary>
        /// Executed on each chat message received and parsed by the Irc client
        /// </summary>
        /// <param name="source">The IrcClient instance the call was made from</param>
        void _ircClient_ChatMessageReceived(object source, ChatMessageReceivedEventArgs e)
        {
            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Message: " + e.UserName + ": " + e.Message);

            AddMessage(e.UserName, e.Message, false);
        }

        /// <summary>
        /// Add a message to chirper queue
        /// </summary>
        private void AddMessage(QueuedChirperMessage message, bool isCritical = false, bool isNewSubscriber = false)
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
                priority = MessagePriority.RabidCrab;
                isCritical = true;
            }
            else if (isCritical)
                priority = MessagePriority.Critical;
            else if (username == _ircClient.UserName.ToLowerInvariant())
            {
                priority = MessagePriority.ModUser;
                isCritical = true;
            }
            else if (isNewSubscriber)
                priority = MessagePriority.NewSubscriber;
            else if (_ircClient.Moderators.ContainsKey(username))
                priority = MessagePriority.Moderator;
            else if (_ircClient.Subscribers.ContainsKey(username))
                priority = MessagePriority.Subscriber;

            if (IsPaused && !isCritical)
                return;

            _messageQueue.Add(new KeyValuePair<MessagePriority, QueuedChirperMessage>(priority, message));

        }

        /// <summary>
        /// Add a message to chirper
        /// </summary>
        private void AddMessage(string citizenName, string text, bool isCritical = false, bool isNewSubscriber = false)
        {
            if (IsPaused && !isCritical)
                return;

            if (text.Length > 160)
                return;

            // The citizen ID will not be pulled until the message makes it through the queue
            AddMessage(new QueuedChirperMessage(citizenName, text), isCritical, isNewSubscriber);
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
                if (_adminClient != null)
                    _adminClient.Dispose();

                _adminClient = null;

                // Make sure the Irc client is really gone so it releases the Irc port
                if (_ircClient != null)
                    _ircClient.Dispose();

                _ircClient = null;
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
        /// OnUpdate is called on the primary thread. When I need to work on non-interface stuff, such as diving down and manually removing
        /// a Chirp, I do it on the OnUpdate call to make sure I have ownership of the main thread. That guarantees I won't step on any toes
        /// while modifying game objects. Mostly copied from https://github.com/mabako/reddit-for-city-skylines/
        /// </summary>
        /*public override void OnUpdate()
        {
            // If there's no message to remove, continue on
            if (_removalTargetCitizenMessage == null)
                return;


            // This code is roughly based on the work by Juuso "Zuppi" Hietala.
            // Get the Chirper container, where all of the chirps reside
            var container = ChirpPanel.instance.transform.FindChild("Chirps").FindChild("Clipper").FindChild("Container").gameObject.transform;
            for (var i = 0; i < container.childCount; ++i)
            {
                // Keep looping until we get the one we want. It should pretty much be the very first one we snag every time, on very rare
                // occurence the second one
                if (!container.GetChild(i).GetComponentInChildren<UILabel>().text.Equals(_removalTargetCitizenMessage.GetText()))
                    continue;

                // Reset the notification sound back to normal
                //ChirpPanel.instance.m_NotificationSound = _messageSound;

                // Remove the message
                UITemplateManager.RemoveInstance("ChirpTemplate", container.GetChild(i).GetComponent<UIPanel>());
                MessageManager.instance.DeleteMessage(_removalTargetCitizenMessage);

                // I'm actually not sure what this does. I don't even have a good guess and my Google-fu failed me
                ChirpPanel.instance.Collapse();

                break;
            }

            // We're done here
            _removalTargetCitizenMessage = null;
        }*/

        /// <summary>
        /// On a new message event we check and see if the message is one we want to keep or delete. Mostly copied from https://github.com/mabako/reddit-for-city-skylines/
        /// </summary>
        /*public override void OnNewMessage(IChirperMessage message)
        {
            // If the message should be filtered, target it for removal
            var citizenMessage = message as CitizenMessage;
            if (citizenMessage == null) return;
            if (!ShouldFilter(citizenMessage.m_messageID)) return;

            // No twitch users can have a space in their name. This will work for now
            if (!message.senderName.Contains(' ')) return;

            // Make sure the user doesn't get an empty notification call because we're getting rid of this message.
            // This needs to be done on the update thread though, so we toss it into a variable for later processing
            //ChirpPanel.instance.m_NotificationSound = null;

            // Make sure it's not already getting worked on. Very, very unlikely but possible. It's fine to wait since
            // we're not on a primary thread
            lock (_removalTargetCitizenMessage)
            {
                _removalTargetCitizenMessage = citizenMessage;
            }
        }*/

        /// <summary>
        /// Decides what should be filtered out. Eventually I'm going to filter out all of the non-Twitch chirps. For now I'm still debugging stuff
        /// </summary>
        /// <param name="message">The text content of the chirp</param>
        /// <returns>true if the message should be removed</returns>
        private static bool ShouldFilter(string message)
        {
            // If the parse throws a null exception, it didn't find it in the enum
            try
            {
                var unused = Enum.Parse(typeof (LocaleID), message);

                return true;
            }
            catch (ArgumentNullException)
            {
                return false;
            }
        }

        /// <summary>
        /// Get the Citizen or rename one and return it. This method really shouldn't be here, but I haven't had the opportunity to move it yet. Mostly copied from
        /// https://github.com/mabako/reddit-for-city-skylines/
        /// </summary>
        /// <param name="name">The name of the Citizen to return</param>
        /// <returns>A citizen with the name to create a Chirper method from</returns>
        private uint LookupCitizenId(string name)
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
                DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format("[TwitchChriperChat] Failed to pick random citizen name for {0}", name));
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
        protected virtual T GetPrivateVariable<T>(object obj, string fieldName)
        {
            var fieldInfo = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (fieldInfo != null)
                return (T)fieldInfo.GetValue(obj);
            else
                throw new ArgumentNullException("(" + fieldName + ") is null!");
        }
    }

    public enum MessagePriority
    {
        RabidCrab = 1, // Not actually used, but it's here for future updates
        Critical = 2, // Program exceptions will be posted via here
        ModUser = 3, // The user themselves
        NewSubscriber = 4, // Yay, new/repeat subscribers!
        Moderator = 5, 
        Subscriber = 6,
        GeneralChat = 7, // General chat is cleared out after each chat grab
    }
}
