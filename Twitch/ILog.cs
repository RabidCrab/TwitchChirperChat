using System;

namespace Twitch
{
    public interface ILog
    {
        void AddEntry(Exception e);
    }
}
