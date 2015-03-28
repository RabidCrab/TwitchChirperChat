using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using TwitchChirperChat.TwitchIrc;

namespace TwitchChirperChat.Twitch.TwitchIrc
{
    /// <summary>
    /// Client for Twitch Irc to communicate with Twitch chat
    /// </summary>
    public class TwitchIrcClient : IDisposable
    {
        private Thread _workerThread;

        /// <summary>
        /// All the login information for Irc
        /// </summary>
        public string UserName { get; private set; }
        private string _oauthToken;
        private string _channel;

        private TcpClient _tcpClient = new TcpClient();
        private TextReader _inputReader;
        private TextWriter _outputWriter;

        public bool IsConnected
        {
            get
            {
                return _tcpClient != null && _tcpClient.Connected;
            }
        }

        /// <summary>
        /// When the thread should self-destruct, this is set to true. Volatile notifies the compiler it will be accessed
        /// by multiple threads
        /// </summary>
        private volatile bool _shouldStop;

        #region Events
        /// <summary>
        /// Triggers on a channel message or private message
        /// </summary>
        public event ChatMessageReceivedHandler ChatMessageReceived;

        /// <summary>
        /// Triggers when there's a new subscriber. This includes re-subscriptions
        /// </summary>
        public event NewSubscriberHandler NewSubscriber;

        /// <summary>
        /// Triggers when there's a new subscriber. This includes re-subscriptions
        /// </summary>
        public event ConnectedHandler Connected;

        /// <summary>
        /// Triggers when the client disconnects. When due to exception, it already logs it
        /// </summary>
        public event DisconnectedHandler Disconnected;
        #endregion

        #region User Lists
        /// <summary>
        /// A list of all the subscribers in the channel. The key is the username, the TwitchUser is not necessarily unique
        /// </summary>
        public Dictionary<string, TwitchUser> Subscribers { get; private set; }

        /// <summary>
        /// A list of all the moderators in the channel. The key is the username, the TwitchUser is not necessarily unique
        /// </summary>
        public Dictionary<string, TwitchUser> Moderators { get; private set; }

        /// <summary>
        /// A list of all the users in the channel. This includes Moderators and Subscribers. The key is the username, the TwitchUser is not necessarily unique
        /// </summary>
        public Dictionary<string, TwitchUser> LoggedInUsers { get; private set; }
        #endregion

        /// <summary>
        /// The starting position for the Twitch Irc client. After instantiation, hook onto the events you want
        /// and call the Connect method
        /// </summary>
        public TwitchIrcClient()
        {
            _workerThread = new Thread(this.DoWork);
            Subscribers = new Dictionary<string, TwitchUser>();
            Moderators = new Dictionary<string, TwitchUser>();
            LoggedInUsers = new Dictionary<string, TwitchUser>();
        }

        /// <summary>
        /// Send a message through the channel
        /// </summary>
        /// <param name="message">The message to send</param>
        public void SendMessage(string message)
        {
            if (_outputWriter != null && IsConnected)
            {
                // Lets me know it's being used at least
                _outputWriter.Write(
                    "PRIVMSG #" + _channel + " :" + UserName + " - " + message + "\r\n"
                );
                _outputWriter.Flush();
            }
        }

        /// <summary>
        /// Connect to Twitch Irc and begin reading/writing to the Irc
        /// </summary>
        /// <param name="userName">The Twitch username. This is case sensitive! If you don't know the proper casing, do all lowercase!</param>
        /// <param name="password">The password always starts with oauth. If you do not have an oauth token, go to http://twitchapps.com/tmi/ and make one</param>
        /// <param name="channel">The channel you want to connect to. For example, if you want to watch TheOddOne's channel, you'd pass #theoddone</param>
        public void Connect(string userName, string password, string channel)
        {
            // For the userName I didn't automatically move it to all lowercase because some people want their names cased properly. The problem is that this
            // will likely be a sticking point for people. I'll make sure to litter the documentation with this caveat
            UserName = userName;
            _oauthToken = password;
            _channel = channel.ToLowerInvariant();

            _workerThread.Start();
        }

        /// <summary>
        /// Reconnect to Twitch Irc and begin reading/writing to the Irc
        /// </summary>
        /// <param name="userName">The Twitch username. This is case sensitive! If you don't know the proper casing, do all lowercase!</param>
        /// <param name="password">The password always starts with oauth. If you do not have an oauth token, go to http://twitchapps.com/tmi/ and make one</param>
        /// <param name="channel">The channel you want to connect to. For example, if you want to watch TheOddOne's channel, you'd pass #theoddone</param>
        public void Reconnect(string userName, string password, string channel)
        {
            _shouldStop = true;

            // For the userName I didn't automatically move it to all lowercase because some people want their names cased properly. The problem is that this
            // will likely be a sticking point for people. I'll make sure to litter the documentation with this caveat
            UserName = userName;
            _oauthToken = password;
            _channel = channel.ToLowerInvariant();

            _workerThread.Join();
            _shouldStop = false;
            _tcpClient = new TcpClient();
            Subscribers = new Dictionary<string, TwitchUser>();
            Moderators = new Dictionary<string, TwitchUser>();
            LoggedInUsers = new Dictionary<string, TwitchUser>();
            _workerThread = new Thread(this.DoWork);
            _workerThread.Start();
        }

        /// <summary>
        /// Run once the thread begins, this is where the magic happens
        /// </summary>
        [SuppressMessage("ReSharper", "JoinDeclarationAndInitializer")] // inputReader and outputWriter can't have their declarations joined due to the tcpClient needing to connect first
        private void DoWork()
        {
            try
            {
                //Connect to irc server and get input and output text streams from TcpClient.
                _tcpClient.Connect("irc.twitch.tv", 6667);

                // Uh-oh we're in trouble
                if (!_tcpClient.Connected)
                    throw new ConnectionFailedException();

                // Yay we're connected
                if (Connected != null) Connected(this, new ConnectedEventArgs());

                _inputReader = new StreamReader(_tcpClient.GetStream());
                _outputWriter = new StreamWriter(_tcpClient.GetStream());

                // Pass the user and oauth token out
                _outputWriter.Write(
                    "PASS " + _oauthToken + "\r\n" +
                    "NICK " + UserName + "\r\n"
                );
                _outputWriter.Flush();

                // Begin the loop to listen for input from the Tcp connection
                while (!_shouldStop)
                {
                    // End the thread if the stop is called
                    if (_shouldStop)
                        break;

                    var buffer = "";

                    if (_tcpClient.GetStream().DataAvailable)
                        buffer = _inputReader.ReadLine();
                    else
                    {
                        Thread.Sleep(1);
                        continue;
                    }
                    // If there's nothing to do here, sleep a moment and continue on
                    if (String.IsNullOrEmpty(buffer))
                        continue;

                    // Send a pong reply to any ping messages, otherwise we'll be disconnected prematurely
                    if (buffer.StartsWith("PING ")) { _outputWriter.Write(buffer.Replace("PING", "PONG") + "\r\n"); _outputWriter.Flush(); }

                    // We don't care about anything but communication attempts, which all start with a :
                    if (buffer[0] != ':') continue;

                    // Parse the message we pulled from the Irc server
                    ParseIrcMessage(buffer, _outputWriter);
                }

                // Disconnect the client
                try
                {
                    _outputWriter.Write("QUIT\r\n");
                    _outputWriter.Flush();
                }
                // If the quit attempt fails, it's not really an issue, just get rid of the Tcp stream next, we'll time out on Irc soon enough
                catch (Exception) {/* ignored */}

                try
                {
                    _tcpClient.GetStream().Close();
                    _tcpClient.Close();
                    _inputReader.Dispose();
                    _outputWriter.Dispose();
                }
                // Anything that goes wrong will be fixed when the application completely dies in a few seconds anyways
                catch (Exception) {/* ignored */}
            }
            catch (Exception ex)
            {
                // Anything that happens outside of the expected scope is going to get logged and the rest of the program will be notified
                // it disconnected with an exception
                Log.AddEntry(ex);

                if (Disconnected != null) Disconnected(this, new DisconnectedEventArgs("Exception: " + ex.Message));
            }

            _shouldStop = false;
            // The stream is complete, notify of a clean disconnect
            if (Disconnected != null) Disconnected(this, new DisconnectedEventArgs(null));
        }

        /// <summary>
        /// Parse the message provided through the buffer
        /// </summary>
        /// <param name="buffer">The string content of the Irc communication</param>
        /// <param name="outputWriter">The TextWriter that we can use to pass information back to the Irc server</param>
        private void ParseIrcMessage(string buffer, TextWriter outputWriter)
        {
            // Irc splits information via spacebars. Irritating as hell IMO
            var splitText = buffer.Split(' ');

            switch (splitText[1])
            {
                // 001 is notification that it was a successful login. We have no interest
                // in the MOTD, but it'd be after 001 if you want to get it. The documentation of what spews out of Twitch Irc
                // is at https://github.com/justintv/Twitch-API/blob/master/IRC.md
                // If you want to read multiple channels at a time, turn _channel into a list and iterate the output.Write + output.Flush
                // for each channel
                case "001":
                    // Lets me know it's being used at least
                    if (_channel != "chirpertestclient")
                    {
                        outputWriter.Write(
                            "PRIVMSG #chirpertestclient :" + UserName + " started mod\r\n"
                            );
                    }
                    outputWriter.Flush();
                    outputWriter.Write(
                        //"MODE " + UserName + "\r\n" +
                        "JOIN " + "#" + _channel + "\r\n"
                    );
                    outputWriter.Flush();
                    TwitchUser loginSuccessfulTwitchUser;
                        // The user won't always be here. If someone immediately joins and says "Hey guys!", they probably won't actually be in here.
                        // Twitch queues all the joins and sends them out every 10 seconds, so my program might not have gotten the memo yet
                    if (!LoggedInUsers.TryGetValue("twitch", out loginSuccessfulTwitchUser))
                        {
                            loginSuccessfulTwitchUser = new TwitchUser() { UserName = "twitch" };
                            LoggedInUsers.Add(loginSuccessfulTwitchUser.UserName, loginSuccessfulTwitchUser);
                        }

                    if (ChatMessageReceived != null) ChatMessageReceived(this, new ChatMessageReceivedEventArgs(new IrcMessage(loginSuccessfulTwitchUser, UserName, _channel, "Login successful! Currently logged in as " + UserName + " and listening to " + _channel, true)));
                     
                    break;
                // Looks like login failed
                case "NOTICE":
                    var noticeMessage = buffer.Substring(NthIndexOf(buffer, ":", 2) + 1, (buffer.Length - NthIndexOf(buffer, ":", 2)) - 1);

                    if (noticeMessage.Contains("Login unsuccessful"))
                    {
                        TwitchUser targetTwitchUser;
                        // The user won't always be here. If someone immediately joins and says "Hey guys!", they probably won't actually be in here.
                        // Twitch queues all the joins and sends them out every 10 seconds, so my program might not have gotten the memo yet
                        if (!LoggedInUsers.TryGetValue("twitch", out targetTwitchUser))
                        {
                            targetTwitchUser = new TwitchUser() { UserName = "twitch" };
                            LoggedInUsers.Add(targetTwitchUser.UserName, targetTwitchUser);
                        }

                        if (ChatMessageReceived != null) ChatMessageReceived(this, new ChatMessageReceivedEventArgs(new IrcMessage(targetTwitchUser, UserName, _channel, "Login failed! Are you sure you have the right username and oauth key?", true)));
                        if (Disconnected != null) Disconnected(this, new DisconnectedEventArgs("Login failed!"));
                    }
                    break;
                // Time to start getting all of the logged in users
                case "353":
                    if (_channel == "chirpertestclient")
                        break;
                    // The first one starts out with a : on their name. The easiest way to get rid of it is to manage it first before any
                    // looping is done
                    LoggedInUsers.Add(splitText[5].Replace(":", ""), new TwitchUser() { UserName = splitText[5].Replace(":", "") });
                    for (var i = 6; i < splitText.Count(); i++)
                    {
                        if (!LoggedInUsers.ContainsKey(splitText[i]))
                            LoggedInUsers.Add(splitText[i], new TwitchUser() { UserName = splitText[i] });
                    }
                    break;
                // Now for the mods
                case "MODE":
                    if (_channel == "chirpertestclient")
                        break;
                    // Hey, a moderator
                    if (splitText[3] == "+o")
                        if (!Moderators.ContainsKey(splitText[4]))
                            Moderators.Add(splitText[4], new TwitchUser() { UserName = splitText[4] });
                    break;
                // Hey, someone's joining
                case "JOIN":
                    if (_channel == "chirpertestclient")
                        break;
                    var joiningUser = splitText[0].Substring(1, splitText[0].IndexOf("!", StringComparison.Ordinal) - 1);
                    // We don't count ourselves because our leave/join event is already obvious to the program. The "353" call above will
                    // have our name in it, so we want to skip it here
                    if (String.Compare(joiningUser, UserName, StringComparison.Ordinal) != 0)
                        // And make sure the list doesn't already have them, or the program will crash on a duplicate insert attempt
                        if (!LoggedInUsers.ContainsKey(joiningUser))
                            LoggedInUsers.Add(joiningUser, new TwitchUser() { UserName = joiningUser });
                    break;
                // Ruh roh, we lost someone
                case "PART":
                    if (_channel == "chirpertestclient")
                        break;
                    var leavingUser = splitText[0].Substring(1, splitText[0].IndexOf("!", StringComparison.Ordinal) - 1);
                    // If the key doesn't exist no exception is thrown
                    LoggedInUsers.Remove(leavingUser);
                    break;
                // PRIVMSG isn't a private message necessarily. It can be either a channel or a user message.
                // Currently I only care about channel messages, but I don't filter out personal messages, they just get put
                // into the same queue
                case "PRIVMSG":
                    // My arch nemesis, Substring. As a warning if you edit this, Substring is a pain in the ass. The first parameter is where 
                    // in the string starts. The second parameter is how long it should read for, NOT THE END POINT IT SHOULD READ TO!
                    var userName = splitText[0].Substring(1, splitText[0].IndexOf("!", StringComparison.Ordinal) - 1);
                    var message = buffer.Substring(NthIndexOf(buffer, ":", 2) + 1, (buffer.Length - NthIndexOf(buffer, ":", 2)) - 1);

                    // Don't care about chirper chat messages not from me
                    if (_channel == "chirpertestclient" && userName != "rabidcrabgt" && !Regex.IsMatch(message, "#moddev", RegexOptions.IgnoreCase))
                        break;

                    // It could be a twitch notification we care about, let's check it out
                    if (userName == "twitchnotify")
                    {
                        // A new or repeat subscriber, sweet
                        if (buffer.Contains("subscribed"))
                        {
                            TwitchUser newSubscriber;
                            var subscriberUserName = message.Split(' ')[0];
                            int monthsSubscribed = 1;

                            // A repeat subscriber, let's show them we care
                            if (buffer.Contains("months in a row"))
                                monthsSubscribed = int.Parse(message.Split(' ')[3]);

                            // It can be either a new subscription or a repeat one. If it's a repeat, we'll just change the month count
                            if (!Subscribers.ContainsKey(subscriberUserName))
                            {
                                newSubscriber = new TwitchUser()
                                {
                                    UserName = subscriberUserName,
                                    SubscribeDateTime = DateTime.Now,
                                    MonthsSubscribed = monthsSubscribed
                                };

                                Subscribers.Add(subscriberUserName, newSubscriber);
                            }
                            else
                            {
                                // So it's a repeat subscriber. Honestly this shouldn't ever happen, because they'd have to run the game for a whole
                                // month before this is relevant
                                if (Subscribers.TryGetValue(subscriberUserName, out newSubscriber))
                                {
                                    newSubscriber.MonthsSubscribed = monthsSubscribed;
                                }
                            }

                            if (NewSubscriber != null) NewSubscriber(this, new NewSubscriberEventArgs(newSubscriber));
                        }
                    }

                    // If there's someone to send it to, pretty it up and send it out
                    if (ChatMessageReceived != null)
                    {
                        TwitchUser targetTwitchUser;
                        // The user won't always be here. If someone immediately joins and says "Hey guys!", they probably won't actually be in here.
                        // Twitch queues all the joins and sends them out every 10 seconds, so my program might not have gotten the memo yet
                        if (!LoggedInUsers.TryGetValue(userName, out targetTwitchUser))
                        {
                            targetTwitchUser = new TwitchUser() { UserName = userName };
                            LoggedInUsers.Add(targetTwitchUser.UserName, targetTwitchUser);
                        }

                        IrcMessage ircMessage = new IrcMessage(targetTwitchUser, UserName, _channel, message);
                        ChatMessageReceived(this, new ChatMessageReceivedEventArgs(ircMessage));
                    }
                    break;
            }
        }

        /// <summary>
        /// Pulled from http://stackoverflow.com/questions/186653/c-sharp-indexof-the-nth-occurrence-of-a-string
        /// It does what it says, and I use it to iterate over the ':' and ' ' markers in Irc messages
        /// </summary>
        /// <param name="target">The target string</param>
        /// <param name="value">The value you want to iterate over</param>
        /// <param name="n">How many times you want to iterate over the value</param>
        /// <returns>The index of the value in target skipped n times</returns>
        private static int NthIndexOf(string target, string value, int n)
        {
            var match = Regex.Match(target, "((" + Regex.Escape(value) + ").*?){" + n + "}");

            if (match.Success)
                return match.Groups[2].Captures[n - 1].Index;

            return -1;
        }

        public void Dispose()
        {
            // Set the stop to get the thread to self-destruct
            _shouldStop = true;
        }
    }

    #region Event ChatMessageReceived
    public delegate void ChatMessageReceivedHandler(object source, ChatMessageReceivedEventArgs e);

    public class ChatMessageReceivedEventArgs : EventArgs
    {
        public IrcMessage Message;

        public ChatMessageReceivedEventArgs(IrcMessage message)
        {
            Message = message;
        }
    }
    #endregion

    #region Event NewSubscriber
    public delegate void NewSubscriberHandler(object source, NewSubscriberEventArgs e);

    public class NewSubscriberEventArgs : EventArgs
    {
        public TwitchUser User { get; private set; }

        public NewSubscriberEventArgs(TwitchUser user)
        {
            User = user;
        }
    }
    #endregion

    #region Event Connected
    public delegate void ConnectedHandler(object source, ConnectedEventArgs e);

    public class ConnectedEventArgs : EventArgs { }
    #endregion

    #region Event Disconnected
    public delegate void DisconnectedHandler(object source, DisconnectedEventArgs e);

    public class DisconnectedEventArgs : EventArgs
    {
        /// <summary>
        /// If the client disconnected due to an unexpected exception, it will be passed here. A null value means a clean disconnect
        /// </summary>
        public string Message { get; private set; }

        public DisconnectedEventArgs(string message)
        {
            Message = message;
        }
    }
    #endregion
}
