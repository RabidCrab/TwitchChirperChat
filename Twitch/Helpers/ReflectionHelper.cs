using System;
using System.Reflection;

namespace TwitchChirperChat.Twitch.Helpers
{
    internal static class ReflectionHelper
    {
        /// <summary>
        /// Get private assembly fields. Copied from https://github.com/mabako/reddit-for-city-skylines/
        /// </summary>
        internal static T GetPrivateVariable<T>(object obj, string fieldName)
        {
            var fieldInfo = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (fieldInfo != null)
                return (T)fieldInfo.GetValue(obj);
            else
                throw new ArgumentNullException("(" + fieldName + ") is null!");
        }

        /// <summary>
        /// Set private assembly fields. Copied from https://github.com/mabako/reddit-for-city-skylines/
        /// </summary>
        internal static void SetPrivateVariable<T>(object obj, string fieldName, T val)
        {
            var fieldInfo = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            fieldInfo.SetValue(obj, val);
        }
    }
}
