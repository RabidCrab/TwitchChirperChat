﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Timers;
using ColossalFramework.Plugins;
using TwitchChirperChat.SimpleJson;
using TwitchChirperChat.TwitchIrc;

namespace TwitchChirperChat.Twitch.TwitchApi
{
    public class TwitchApiManager : IDisposable
    {
        private readonly Timer _callTimer;
        private volatile bool _isFirstCall = true;

        public string Channel { get; private set; }

        private readonly List<TwitchUser> _followers = new List<TwitchUser>();
        /// <summary>
        /// List of current stream followers
        /// </summary>
        public List<TwitchUser> Followers
        {
            get { return _followers; }
        }

        /// <summary>
        /// Triggers when there's new followers
        /// </summary>
        public event NewFollowersHandler NewFollowers;

        public TwitchApiManager(string channel)
        {
            Channel = channel;

            _callTimer = new Timer(120000) { AutoReset = true };
            _callTimer.Elapsed += _callTimer_Elapsed;
        }

        /// <summary>
        /// I wasted 2 hours on this. Twitch uses https. Debugging in visual studio used windows certs. Debugging in the game used an
        /// empty list of certs. This just bypasses the check and allows the communication. Not really all that safe
        /// </summary>
        public static bool Validator(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        /// <summary>
        /// Call the server and get the first 25 followers. After that we call every once in a while to watch for new ones
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _callTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // This isn't necessary for the program to keep running. If it fails catch the error and just keep going
            try
            {
                string followers = GetFollowers(@"https://api.twitch.tv/kraken/channels/" + Channel + @"/follows?limit=10");

                JsonObject root = (JsonObject)SimpleJson.SimpleJson.DeserializeObject(followers);
                JsonArray follows = (JsonArray)root["follows"];

                List<TwitchUser> newFollowers = new List<TwitchUser>();

                // Add all the followers, and record the new ones in newFollowers to be passed through the event
                foreach (object obj in follows)
                {
                    JsonObject follow = (JsonObject)obj;
                    JsonObject user = (JsonObject)follow["user"];

                    DateTime followDateTime;
                    if (DateTime.TryParse(follow["created_at"].ToString(), out followDateTime))
                    {
                        var newFollower = new TwitchUser()
                        {
                            UserName = user["display_name"].ToString(),
                            FollowedDateTime = followDateTime,
                        };
                    
                        if (Followers.Contains(newFollower))
                            continue;

                        Followers.Add(newFollower);
                        newFollowers.Add(newFollower);
                    }
                    else
                    {
                        DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Can't parse " + follow["created_at"].ToString() + " to a DateTime");
                    }
                }

                if (newFollowers.Count > 0 && !_isFirstCall)
                    if (NewFollowers != null) 
                        NewFollowers(this, new NewFollowersEventArgs(newFollowers));

                _isFirstCall = false;
            }
            catch (Exception ex)
            {
                Log.AddEntry(ex);
            }
        }

        /// <summary>
        /// Get the string result of followers
        /// </summary>
        /// <param name="url"></param>
        /// <returns>string of followers, ready to be parsed</returns>
        private string GetFollowers(string url)
        {
            // We have to authenticate to get information back
            ServicePointManager.ServerCertificateValidationCallback += Validator;

            var request = (HttpWebRequest) WebRequest.Create(url);
            request.ContentType = "application/json";
            request.Accept = "application/vnd.twitchtv.v3+json";
            request.Method = "GET";

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var responseString = reader.ReadToEnd();

                    return responseString;
                }
            }
        }

        public void StartWatching()
        {
            _callTimer.Start();

            _callTimer_Elapsed(null, null);
        }

        public void Dispose()
        {
            try
            {
                _callTimer.Dispose();
            }
            // ReSharper disable once EmptyGeneralCatchClause - No need to worry about the timer not dying as intended
            catch {}
        }
    }

    #region Event NewSubscriber
    public delegate void NewFollowersHandler(object source, NewFollowersEventArgs e);

    public class NewFollowersEventArgs : EventArgs
    {
        public List<TwitchUser> Users { get; private set; }

        public NewFollowersEventArgs(List<TwitchUser> users)
        {
            Users = users;
        }
    }
    #endregion
}
