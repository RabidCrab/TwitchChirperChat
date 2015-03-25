using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using ColossalFramework.IO;
using ColossalFramework.Plugins;

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
            if (!File.Exists(Configuration.ConfigPath))
            {
                ConfigurationSettings = CreateDefaultConfig();
                Serialize(ConfigPath, ConfigurationSettings);
            }
            else
                ConfigurationSettings = Deserialize(ConfigPath);

        }

        /// <summary>
        /// When I serialize the document, I want to add some comments to help people who manually modify the parameters
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="config"></param>
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        internal static void Serialize(string filename, ConfigurationFile config)
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

                // Save it to the file
                using (var fileWriter = System.IO.File.CreateText(filename))
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
            var configurationFile = new ConfigurationFile();

            configurationFile.UserName = "YourUserNameGoesHere";
            configurationFile.OAuthKey = "oauth:000000000000000";
            configurationFile.IrcChannel = "bacon_donut";

            return configurationFile;
        }
    }
}
