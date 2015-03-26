using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using ColossalFramework.IO;

namespace TwitchChirperChat
{
    /// <summary>
    /// Configuration settings for all of the configuration values
    /// </summary>
    public static class Configuration
    {
        private static string ConfigPath
        {
            get
            {
                // base it on the path Cities: Skylines uses
                string path = string.Format("{0}/{1}/", DataLocation.localApplicationData, "ModConfig");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                path += "TwitchChirperChatSettings.xml";

                return path;
            }
        }

        /// <summary>
        /// The loaded configuration settings
        /// </summary>
        internal static ConfigurationFile ConfigurationSettings;

        /// <summary>
        /// Load up the xml file
        /// </summary>
        static Configuration()
        {
            ReloadConfigFile();
        }

        /// <summary>
        /// Reload the config from the file
        /// </summary>
        public static void ReloadConfigFile()
        {
            if (!File.Exists(ConfigPath))
            {
                ConfigurationSettings = CreateDefaultConfig();
                Serialize(ConfigPath, ConfigurationSettings);
            }
            else
                ConfigurationSettings = Deserialize(ConfigPath);
        }

        /// <summary>
        /// Save the configuration
        /// </summary>
        public static void SaveConfigFile()
        {
            Serialize(ConfigPath, ConfigurationSettings);
        }

        /// <summary>
        /// When I serialize the document, I want to add some comments to help people who manually modify the parameters
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="config"></param>
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private static void Serialize(string filename, ConfigurationFile config)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(ConfigurationFile));

                // Serialize the ConfigurationFile to an XmlDocument
                var doc = new XmlDocument();
                var writer = new StringWriter();
                serializer.Serialize(writer, config);
                doc.LoadXml(writer.ToString());
                
                if (doc.DocumentElement == null)
                    throw new NullReferenceException("The DocumentElement is null for the XML document! This shouldn't be possible");

                // Add the comments
                var userComment = doc.CreateComment("Your username in all lowercase letters goes here. NO SPACES BEFORE OR AFTER");
                doc.DocumentElement.InsertBefore(userComment, FindNode(doc.DocumentElement.ChildNodes, "UserName"));

                var oAuthKeyComment = doc.CreateComment(@"Generate your oauth key and make sure to include the oauth:! https://api.twitch.tv/kraken/oauth2/authorize?response_type=token&client_id=6590rp99k45b8cdaxwa6wa8vhm1ve7e&redirect_uri=http://twitchapps.com/tmi/&scope=chat_login+channel_check_subscription+channel_subscriptions");
                doc.DocumentElement.InsertBefore(oAuthKeyComment, FindNode(doc.DocumentElement.ChildNodes, "OAuthKey"));

                var ircChannelComment = doc.CreateComment("The IRC channel is always the name of the streamer you want to watch. For instance, http://www.twitch.tv/manvsgame would mean I put manvsgame here. Joining multiple channels is not possible at this time");
                doc.DocumentElement.InsertBefore(ircChannelComment, FindNode(doc.DocumentElement.ChildNodes, "IrcChannel"));

                var delayBetweenChirperMessagesComment = doc.CreateComment("How many milliseconds to wait before sending a message. Default is 8000 which is 8 seconds. Minimum is 8");
                doc.DocumentElement.InsertBefore(delayBetweenChirperMessagesComment, FindNode(doc.DocumentElement.ChildNodes, "DelayBetweenChirperMessages"));

                var maximumGeneralChatMessageQueueComment = doc.CreateComment("The maximum number of general chat messagess that can be queued before the queue is cleared. This number includes THE SUM OF subs, mods, and your own chats into the count");
                doc.DocumentElement.InsertBefore(maximumGeneralChatMessageQueueComment, FindNode(doc.DocumentElement.ChildNodes, "MaximumGeneralChatMessageQueue"));

                var maximumSubscriberChatMessageQueueComment = doc.CreateComment("The maximum number of subscriber chat messages that can be queued before the queue is cleared. This number includes THE SUM OF mods, and your own chats into the count");
                doc.DocumentElement.InsertBefore(maximumSubscriberChatMessageQueueComment, FindNode(doc.DocumentElement.ChildNodes, "MaximumSubscriberChatMessageQueue"));

                var maximumModeratorChatMessageQueueComment = doc.CreateComment("The maximum number of subscriber chat messagess that can be queued before the queue is cleared. This number includes your own chats into the count");
                doc.DocumentElement.InsertBefore(maximumModeratorChatMessageQueueComment, FindNode(doc.DocumentElement.ChildNodes, "MaximumModeratorChatMessageQueue"));

                var prioritizePersonallyAddressedMessagesComment = doc.CreateComment("Prioritize any chats with @YourName in them");
                doc.DocumentElement.InsertBefore(prioritizePersonallyAddressedMessagesComment, FindNode(doc.DocumentElement.ChildNodes, "PrioritizePersonallyAddressedMessages"));

                var newSubscriberMessageComment = doc.CreateComment("New sub comment");
                doc.DocumentElement.InsertBefore(newSubscriberMessageComment, FindNode(doc.DocumentElement.ChildNodes, "NewSubscriberMessage"));

                var repeatSubscriberMessageComment = doc.CreateComment("The {0} is the number of months subscribed");
                doc.DocumentElement.InsertBefore(repeatSubscriberMessageComment, FindNode(doc.DocumentElement.ChildNodes, "RepeatSubscriberMessage"));

                var seniorSubscriberMessageComment = doc.CreateComment("The {0} is the number of months subscribed");
                doc.DocumentElement.InsertBefore(seniorSubscriberMessageComment, FindNode(doc.DocumentElement.ChildNodes, "SeniorSubscriberMessage"));

                var showSubscriberMessagesComment = doc.CreateComment("If you want to see the messages above, keep true");
                doc.DocumentElement.InsertBefore(showSubscriberMessagesComment, FindNode(doc.DocumentElement.ChildNodes, "ShowSubscriberMessages"));

                var renameCitizensToLoggedInUsersComment = doc.CreateComment("Highly recommended you do not change this unless there's a conflict with another mod");
                doc.DocumentElement.InsertBefore(renameCitizensToLoggedInUsersComment, FindNode(doc.DocumentElement.ChildNodes, "RenameCitizensToLoggedInUsers"));

                var renameCitizensToFollowersComment = doc.CreateComment("Renames citizens to followers");
                doc.DocumentElement.InsertBefore(renameCitizensToFollowersComment, FindNode(doc.DocumentElement.ChildNodes, "RenameCitizensToFollowers"));

                var showGeneralChatMessagesComment = doc.CreateComment("Show general chat messages in chirper");
                doc.DocumentElement.InsertBefore(showGeneralChatMessagesComment, FindNode(doc.DocumentElement.ChildNodes, "ShowGeneralChatMessages"));

                var showSubscriberChatMessagesComment = doc.CreateComment("Show subscriber chat messages in chirper");
                doc.DocumentElement.InsertBefore(showSubscriberChatMessagesComment, FindNode(doc.DocumentElement.ChildNodes, "ShowSubscriberChatMessages"));

                var showModeratorChatMessagesComment = doc.CreateComment("Show moderator chat messages in chirper");
                doc.DocumentElement.InsertBefore(showModeratorChatMessagesComment, FindNode(doc.DocumentElement.ChildNodes, "ShowModeratorChatMessages"));

                var newFollowersMessageComment = doc.CreateComment("The {0} is a comma delineated list of new followers");
                doc.DocumentElement.InsertBefore(newFollowersMessageComment, FindNode(doc.DocumentElement.ChildNodes, "NewFollowersMessage"));

                var showNewFollowersMessageComment = doc.CreateComment("Show the new followers message");
                doc.DocumentElement.InsertBefore(showNewFollowersMessageComment, FindNode(doc.DocumentElement.ChildNodes, "ShowNewFollowersMessage"));

                var showDefaultChirperMessagesComment = doc.CreateComment("Shows the default game chirper messages");
                doc.DocumentElement.InsertBefore(showDefaultChirperMessagesComment, FindNode(doc.DocumentElement.ChildNodes, "ShowDefaultChirperMessages"));

                var maximumMessageSizeComment = doc.CreateComment("The maximum length a message can be before it's trimmed down");
                doc.DocumentElement.InsertBefore(maximumMessageSizeComment, FindNode(doc.DocumentElement.ChildNodes, "MaximumMessageSize"));
                
                // Save it to the file
                using (var fileWriter = File.CreateText(filename))
                {
                    doc.Save(fileWriter);
                }
            }
            catch (Exception ex)
            {
                Log.AddEntry(ex);
            }
        }

        /// <summary>
        /// Upon deserialization, I want to do some sanity checks to make sure the inputs are all messed up
        /// </summary>
        /// <param name="filename">The location of the file</param>
        /// <returns>The loaded configuration file</returns>
        internal static ConfigurationFile Deserialize(string filename)
        {
            var serializer = new XmlSerializer(typeof(ConfigurationFile));

            try
            {
                using (var reader = new StreamReader(filename))
                {
                    var config = (ConfigurationFile) serializer.Deserialize(reader);

                    if (config.UserName.Contains(' '))
                        throw new InvalidDataException("Your name cannot have spaces!");

                    if (config.UserName.Contains("YourUserNameGoesHere"))
                        throw new InvalidDataException("The UserName is still the default value! Change the UserName to your Twitch account name");

                    // It needs to be done no matter what
                    config.UserName = config.UserName.ToLowerInvariant();

                    if (!config.OAuthKey.Contains("oauth:"))
                        throw new InvalidDataException("Input the whole oauth key, including oauth:! It should look something like oauth:0aaa000aa0aaa00");

                    if (config.OAuthKey.Contains("oauth:000000000000000"))
                        throw new InvalidDataException("The oauth key is still the default value! Read the settings file to learn how to generate a Twitch oauth key!");

                    return config;
                }
            }
            catch (Exception ex)
            {
                Log.AddEntry(ex);
            }

            return null;
        }

        /// <summary>
        /// When the program is run for the first time, or someone deletes the settings file, this is called to pull default values to be saved
        /// to the file location for later use
        /// </summary>
        /// <returns>ConfigurationFile with all default values</returns>
        private static ConfigurationFile CreateDefaultConfig()
        {
            var configurationFile = new ConfigurationFile()
            {
                UserName = "chirpertestclient",
                OAuthKey = "oauth:eqtt3b1vl3dxmthyyzo9l5f2clyj5s",
                IrcChannel = "cleavetv",
                DelayBetweenChirperMessages = 8000,
                MaximumGeneralChatMessageQueue = 20,
                MaximumSubscriberChatMessageQueue = 10,
                MaximumModeratorChatMessageQueue = 10,
                PrioritizePersonallyAddressedMessages = true,
                NewSubscriberMessage = "Hey everyone, I just subscribed! #Newbie #WelcomeToTheParty",
                RepeatSubscriberMessage = "I've just subscribed for {0} months in a row! #BestSupporterEver",
                SeniorSubscriberMessage = "I've been supporting the stream for {0} months in a row! #SeniorDiscount #GetOnMyLevel",
                ShowSubscriberMessages = true,
                RenameCitizensToLoggedInUsers = true,
                RenameCitizensToFollowers = true,
                ShowGeneralChatMessages = true,
                ShowSubscriberChatMessages = true,
                ShowModeratorChatMessages = true,
                NewFollowersMessage = "Welcome {0}, thanks for following!",
                ShowNewFollowersMessage = true,
                ShowDefaultChirperMessages = false,
                MaximumMessageSize = 240,
            };

            return configurationFile;
        }

        /// <summary>
        /// Nodes are not always easy to find. This helper method makes searching for them much easier. Code found at 
        /// http://stackoverflow.com/questions/2797238/search-for-nodes-by-name-in-xmldocument
        /// </summary>
        /// <param name="list">The list of nodes to begin the search</param>
        /// <param name="nodeName">The name of the node to search for</param>
        /// <returns>The target node, or null if not found</returns>
        private static XmlNode FindNode(XmlNodeList list, string nodeName)
        {
            if (list.Count > 0)
            {
                foreach (XmlNode node in list)
                {
                    if (node.Name.Equals(nodeName)) return node;

                    if (node.HasChildNodes)
                    {
                        XmlNode nodeFound = FindNode(node.ChildNodes, nodeName);

                        if (nodeFound != null)
                            return nodeFound;
                    }
                }
            }

            return null;
        }
    }
}
