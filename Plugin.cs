using BepInEx;
using BepInEx.Configuration;
using Cwl.API.Attributes;
using Cwl.API.Processors;
using Elin.Gxt;
using Elin.Gxt.Advisory;
using HarmonyLib;
using Newtonsoft.Json;
using ReflexCLI.Attributes;
using Steamworks;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MyMod;
public static class ModInfo
{
    public const string Guid = "iah.gxt.IdCard";
    public const string Name = "GXT ID Card";
    public const string Version = "1.0.0";
}

class IdCardMeta
{
    public string CharacterName { get; set; } = Game.pc.NameSimple;
    public string SteamName { get; set; } = SteamFriends.GetPersonaName();
    public string Portrait { get; set; } = Game.pc.GetIdPortrait();
    public int CurrentLevel { get; set; } = Game.pc.LV;
    public string Race { get; set; } = Game.pc.race.GetName();
}

class KeyWrapper
{
    public string Key { get; set; }
    public string IdCard { get; set; }
}

[HarmonyPatch(typeof(Game), nameof(Game.StartNewGame))]
class StartNewGamePatch
{
    static void Postfix()
    {
        Plugin.MakeIdentity(KeyCreation.WithKey);
    }
}

[HarmonyPatch(typeof(Game), nameof(Game.OnLoad))]
class LoadGamePatch
{
    static void Postfix()
    {
        Plugin.SetSavePaths();

        if (!File.Exists(Plugin.KeyPath))
        {
            try
            {
                Plugin.MakeIdentity(KeyCreation.WithKey);
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
                Plugin.MakeIdentity(KeyCreation.WithoutKey);
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

enum KeyCreation
{
    WithKey,
    WithoutKey,
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class Plugin : BaseUnityPlugin
{
    internal static Plugin? Instance;
    internal static KeyWrapper Identity = null;
    internal static string KeyPath = "";
    internal static string IdCardPath = "";

    internal static void SaveKey()
    {
        File.WriteAllText(KeyPath, Identity.Key, System.Text.Encoding.ASCII);
    }

    internal static void SaveIdCard()
    {
        File.WriteAllText(IdCardPath, Identity.IdCard, System.Text.Encoding.ASCII);
    }

    internal static void SetSavePaths()
    {
        KeyPath = Path.Combine(GameIO.pathCurrentSave, "key.gxk");
        IdCardPath = Path.Combine(GameIO.pathCurrentSave, "id.gxi");
    }

    internal static void MakeIdentity(KeyCreation withKey)
    {
        SetSavePaths();
        try
        {
            string key;
            if (withKey == KeyCreation.WithKey)
            {
                key = GxtSdk.MakeKey();
            }
            else
            {
                key = Identity.Key;
            }
            var idCard = GxtSdk.MakeIdCard(key, new IdCardMeta());
            Identity = new KeyWrapper { Key = key, IdCard = idCard };
            SaveKey();
            SaveIdCard();
        }
        catch (FfiException e)
        {
            LogError($"GXT Error Code: {e.ErrorCode}");
        }

    }

    [CwlContextMenu("gxt_ui_id_card_show")]
    internal static void ShowIdCard()
    {
        // Hacky, but works for now
        Process.Start("notepad.exe", $"\"{IdCardPath}\"");
    }

    [CwlContextMenu("gxt_ui_id_card_copy")]
    internal static void CopyIdToClipboard()
    {
        GUIUtility.systemCopyBuffer = Identity.IdCard;
    }

    public static void Say(string text)
    {
        Msg.SetColor(Msg.colors.TalkGod);
        Msg.Say(text);
    }

    private void Awake()
    {
        Instance = this;
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModInfo.Guid);
    }

    internal static void LogDebug(object message, [CallerMemberName] string caller = "")
    {
        Instance?.Logger.LogDebug($"[{caller}] {message}");
    }

    internal static void LogInfo(object message, [CallerMemberName] string caller = "")
    {
        Instance?.Logger.LogInfo($"[{caller}] {message}");
    }

    internal static void LogError(object message, [CallerMemberName] string caller = "")
    {
        Instance?.Logger.LogError($"[{caller}] {message}");
    }
}