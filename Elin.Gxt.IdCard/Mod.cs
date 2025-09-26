using BepInEx;
using HarmonyLib;
using System.Reflection;
using BepInEx.Logging;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace ElinGxtIdCard.Mod;
#pragma warning restore IDE0130 // Namespace does not match folder structure

internal static class ModInfo
{
    public const string Guid = "iah.gxt.IdCard";
    public const string Name = "GXT ID Card";
    public const string Version = "1.0.0";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class Plugin : BaseUnityPlugin
{
    internal static Plugin? Instance;
    internal static ManualLogSource? Log
    {
        get {
            return Instance?.Logger;
        }
    }

    internal void Awake()
    {
        Instance = this;
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModInfo.Guid);
    }
}