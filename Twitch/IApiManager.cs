using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Twitch
{
    internal interface IApiManager : IDisposable
    {
        string Channel { get; }

        ILog Logger { get; }

        List<User> Followers { get; }

        event NewFollowersHandler NewFollowers;

        bool Validator(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors);

        void StartWatching();

        void StopWatching();
    }
}
