using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Twitch;
using TwitchChirperChat.Chirper;
using TwitchChirperChat.Twitch.Helpers;
using TwitchChirperChat.UI;

namespace TwitchChirperChat
{
    /// <summary>
    /// The core of the mod. Most of it is static because I didn't properly plan ahead
    /// </summary>
    public class ChirperExtension : ChirperExtensionBase
    {
        internal static ILog Logger = new Log();

        internal static IIrcClient IrcClient = new IrcClient(Logger);

        /// <summary>
        /// The API manages existing subscribers, and new/existing followers. Everything else is covered by TwitchIrcClient
        /// </summary>
        private static IApiManager _apiManager;

        internal static IChirperManager GetChirperManager = new ChirperManager(IrcClient, Logger);

        /// <summary>
        /// Used for deletion of default messages
        /// </summary>
        private static CitizenMessage _messageRemovalTarget = null;

        public static TwitchChirpPanel _viewingPanel = new TwitchChirpPanel();

        public static Dictionary<string, uint> CustomCitizens { get; private set; }

        /// <summary>
        /// We don't want to queue up a crapton of messages while the game is paused and stuff like that. While paused the message queue will not be added to
        /// unless the message is critical
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
            //c.DestroyBuiltinChirper();

            Configuration.ReloadConfigFile();

            if (IrcClient == null)
                IrcClient = new IrcClient(Logger);

            // The noise will drive people bonkers
            ChirpPanel cp = ChirpPanel.instance;
            if (cp != null)
                cp.m_NotificationSound = null;

            CustomCitizens = GetCustomCitizens();

            //bool enterPressed = Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);

            // If they're using the default username, make them aware of the options tab
            GetChirperManager.AddMessage("chirpertestclient", "Welcome to Twitch Chirper Chat! Click the Options button or press Alt+C to access options!", MessagePriority.Critical);

            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message,
                String.Format("User Name: {0} - Channel: {1} - Connecting to Irc...",
                    Configuration.ConfigurationSettings.UserName,
                    Configuration.ConfigurationSettings.IrcChannel));

            try
            {
                if (_apiManager != null)
                    _apiManager.Dispose();
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch {}

            _apiManager = new ApiManager(Logger, Configuration.ConfigurationSettings.IrcChannel);
            _apiManager.NewFollowers += _apiManager_NewFollowers;
            _apiManager.StartWatching();

            // Hook up the Irc client events and execute a connection
            IrcClient.ChatMessageReceived += _ircClient_ChatMessageReceived;
            IrcClient.Disconnected += _ircClient_Disconnected;
            IrcClient.Connected += _ircClient_Connected;
            IrcClient.NewSubscriber += _ircClient_NewSubscriber;
            IrcClient.Connect(Configuration.ConfigurationSettings.UserName, Configuration.ConfigurationSettings.OAuthKey, Configuration.ConfigurationSettings.IrcChannel);
        }

        private static Dictionary<string, uint> GetCustomCitizens()
        {
            var customCitizens = new Dictionary<string, uint>();
            var citizens = CitizenManager.instance.m_citizens.m_buffer.Select((citizen, citizenId) => new { citizen, citizenId });

            foreach (var citizenPair in citizens)
            {
                // Citizen doesn't exist anymore. Could have died
                if (CitizenManager.instance.m_citizens.m_buffer[citizenPair.citizenId].m_flags == Citizen.Flags.None)
                    continue;

                // Citizen has a custom name
                if ((CitizenManager.instance.m_citizens.m_buffer[citizenPair.citizenId].m_flags & Citizen.Flags.CustomName) != Citizen.Flags.None)
                {
                    customCitizens.Add(CitizenManager.instance.GetCitizenName((uint)citizenPair.citizenId), (uint)citizenPair.citizenId);
                    //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Custom citizen found: " + CitizenManager.instance.GetCitizenName((uint)citizenPair.citizenId));
                }
            }

            return customCitizens;
        }

        private static void _ircClient_Connected(object source, ConnectedEventArgs e)
        {
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Irc connected");
        }

        private static void _ircClient_Disconnected(object source, DisconnectedEventArgs e)
        {
            // If it failed from an exception, clean everything up and notify the user the mod is shutting down
            if (e.Message != null)
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Error! Client was disconnected from Irc for some reason!");
            }
        }

        /// <summary>
        /// Got a new/repeat sub!
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private static void _ircClient_NewSubscriber(object source, NewSubscriberEventArgs e)
        {
            // Make sure the user wants to see new/repeat subs
            if (!Configuration.ConfigurationSettings.ShowSubscriberMessages)
                return;

            if (e.User.MonthsSubscribed < 2)
            {
                GetChirperManager.AddMessage(e.User.UserName, Configuration.ConfigurationSettings.NewSubscriberMessage, MessagePriority.NewSubscriber);
                return;
            }

            if (e.User.MonthsSubscribed < 6)
            {
                GetChirperManager.AddMessage(e.User.UserName,
                    String.Format(Configuration.ConfigurationSettings.RepeatSubscriberMessage, e.User.MonthsSubscribed.ToString()), MessagePriority.NewSubscriber);
                return;
            }

            if (e.User.MonthsSubscribed >= 6)
            {
                GetChirperManager.AddMessage(e.User.UserName, String.Format(Configuration.ConfigurationSettings.SeniorSubscriberMessage, e.User.MonthsSubscribed.ToString()), MessagePriority.NewSubscriber);
                return;
            }
        }

        /// <summary>
        /// New followers from the API
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private static void _apiManager_NewFollowers(object source, NewFollowersEventArgs e)
        {
            if (!Configuration.ConfigurationSettings.ShowNewFollowersMessage)
                return;

            GetChirperManager.AddMessage(Configuration.ConfigurationSettings.IrcChannel,
                // Set the list of new followers to a comma delimited string
                String.Format(Configuration.ConfigurationSettings.NewFollowersMessage, String.Join(", ", e.Users.Select(x => x.UserName).ToArray())),
                MessagePriority.NewFollowers);
        }

        /// <summary>
        /// Executed on each chat message received
        /// </summary>
        /// <param name="source">The IrcClient instance the call was made from</param>
        /// <param name="e">The content of the message</param>
        static void _ircClient_ChatMessageReceived(object source, ChatMessageReceivedEventArgs e)
        {
            // I put up general errors and whatnot in Chirper since my mod specifically modifies it
            if (e.Message.IsUserFeedback)
            {
                ReflectionHelper.SetPrivateVariable<bool>(MessageManager.instance, "m_canShowMessage", true);

                MessageManager.instance.QueueMessage(new Message(e.Message.User.UserName,
                    e.Message.Message, "", GetCitizenId(e.Message.User.UserName)));
            }
            else
            {
                GetChirperManager.AddMessage(e.Message.User.UserName, e.Message.Message, MessagePriority.GeneralChat, e.Message.IsIrcUserMentioned);
            }
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
                    cp.m_NotificationSound = null;

                // Make sure the Irc client is really gone so it releases the Irc port
                if (IrcClient != null)
                    IrcClient.Dispose();

                IrcClient = null;
            }
            catch (Exception ex)
            {
                Logger.AddEntry(ex);
            }
        }

        /// <summary>
        /// OnNewMessage runs before the message can be found in the stack. So we try to get
        /// rid of it here Mostly copied from https://github.com/mabako/reddit-for-city-skylines/
        /// </summary>
        public override void OnUpdate()
        {
            // If there's no message to remove, continue on
            if (_messageRemovalTarget == null)
                return;

            if (ChirpPanel.instance == null) 
                return;

            // This code is roughly based on the work by Juuso "Zuppi" Hietala.
            // Get the Chirper container, where all of the chirps reside
            var container = ChirpPanel.instance.transform.Find("Chirps").Find("Clipper").Find("Container").gameObject.transform;
            for (var i = 0; i < container.childCount; ++i)
            {
                // Keep looping until we get the one we want. It should pretty much be the very first one we snag every time, on very rare
                // occurence the second one
                if (!container.GetChild(i).GetComponentInChildren<UILabel>().text.Equals(_messageRemovalTarget.GetText()))
                    continue;

                // Remove the message
                UITemplateManager.RemoveInstance("ChirpTemplate", container.GetChild(i).GetComponent<UIPanel>());
                MessageManager.instance.DeleteMessage(_messageRemovalTarget);

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
        /// <param name="userName">The name of the Citizen to return</param>
        /// <returns>A citizen with the name to create a Chirper method from</returns>
        internal static uint GetCitizenId(string userName)
        {
            if (string.IsNullOrEmpty(userName)) return 0;

            // Overwrite any CIM's name by their username.
            // To be fair: this was the more interesting part.
            /*
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
                catch (Exception ex)
                {
                    DebugOutputPanel.AddMessage(PluginManager.MessageType.Error, ex.Message);
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
            }
            catch
            {
                // not sure if this would happen often. Who knows.
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, string.Format("[TwitchChriperChat] Failed to pick random citizen name for {0}", name));
            }

            // Either we have no people, or we have some people but couldn't find anyone to use for our purposes,
            // or we don't want people renamed.
            return 0;
            */

            uint citizenId;
            if (CustomCitizens.TryGetValue(userName, out citizenId))
            {
                return citizenId;
            }
            else
            {
                // Loop until we get a resident who isn't dead or otherwise invalid in some way
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
                    CitizenManager.instance.StartCoroutine(CitizenManager.instance.SetCitizenName(id, userName));

                    CustomCitizens.Add(userName, id);

                    return id;
                }

                return 0;
            }
        }
    }

    public enum MessagePriority
    {
        Critical = 1, // Program exceptions will be posted via here
        ModUser = 2, // The user themselves
        NewSubscriber = 3, // Yay, new/repeat subscribers!
        Moderator = 4, 
        NewFollowers = 5,
        Subscriber = 6,
        GeneralChat = 7, // General chat is cleared out after each chat grab
        None = 8,
    }
}
