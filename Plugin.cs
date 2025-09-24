using BepInEx;
using BepInEx.Configuration;
using Gxt.Net;
using Gxt.Net.Advisory;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MyMod;
public static class ModInfo
{
    public const string Guid = "iah.gxt.IdCard";
    public const string Name = "GxtIdCard";
    public const string Version = "1.0.0";
}

class KeyWrapper
{
    public string Key { get; set; }
    public string IdCard { get; set; }
}

[HarmonyPatch(typeof(Game), nameof(Game.OnLoad))]
class LoadGamePatch
{
    static void Postfix()
    {
        Plugin.KeyPath = Path.Combine(GameIO.pathCurrentSave, "key.gxk");
        Plugin.IdCardPath = Path.Combine(GameIO.pathCurrentSave, "id.gxi");

        if (!File.Exists(Plugin.KeyPath))
        {
            try
            {
                var _key = GxtSdk.MakeKey();
                var _idCard = GxtSdk.MakeIdCard(_key, new IdCard { DisplayName = Game.pc.Name });
                Plugin.Identity = new KeyWrapper { Key = _key, IdCard = _idCard };
                File.WriteAllText(Plugin.KeyPath, _key, System.Text.Encoding.UTF8);
                File.WriteAllText(Plugin.IdCardPath, _idCard, System.Text.Encoding.UTF8);
            }
            catch (FfiException e)
            {
                Plugin.LogError(e.ErrorCode);
            }
            return;
        }

        if (!File.Exists(Plugin.IdCardPath))
        {
            try
            {
                var _key = File.ReadAllText(Plugin.IdCardPath);
                var _idCard = GxtSdk.MakeIdCard(_key, new IdCard { DisplayName = Game.pc.Name });
                Plugin.Identity = new KeyWrapper { Key = _key, IdCard = _idCard };
                File.WriteAllText(Plugin.IdCardPath, _idCard, System.Text.Encoding.UTF8);
            }
            catch (FfiException e)
            {
                Plugin.LogError(e.ErrorCode);
            }
            return;
        }

        var key = File.ReadAllText(Plugin.KeyPath);
        var idCard = File.ReadAllText(Plugin.IdCardPath);
        Plugin.Identity = new KeyWrapper { Key = key, IdCard = idCard };
    }
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class Plugin : BaseUnityPlugin
{
    internal static Plugin? Instance;
    internal static KeyWrapper Identity = null;
    internal static string KeyPath = "";
    internal static string IdCardPath = "";

    private void Awake()
    {
        Instance = this;
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModInfo.Guid);
    }

    internal static void LogDebug(object message, [CallerMemberName] string caller = "")
    {
        Instance?.Logger.LogDebug($"[{caller}] {message}");
    }

    internal static void LogInfo(object message)
    {
        Instance?.Logger.LogInfo(message);
    }

    internal static void LogError(object message)
    {
        Instance?.Logger.LogError(message);
    }
}