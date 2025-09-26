using ElinGxtIdCard.Mod;
using System.Runtime.CompilerServices;

namespace ElinGxtIdCard
{
    internal static class Logger
    {
        internal static void Debug(object message, [CallerMemberName] string caller = "")
        {
            Plugin.Log?.LogDebug($"[{caller}] {message}");
        }

        internal static void Info(object message, [CallerMemberName] string caller = "")
        {
            Plugin.Log?.LogInfo($"[{caller}] {message}");
        }

        internal static void Error(object message, [CallerMemberName] string caller = "")
        {
            Plugin.Log?.LogError($"[{caller}] {message}");
        }
    }
}
