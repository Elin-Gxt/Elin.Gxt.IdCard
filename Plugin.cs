using BepInEx;
using Cwl.API.Attributes;
using Cwl.API.Processors;
using Elin.Gxt;
using HarmonyLib;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using YKF;

namespace MyMod;

using static Widget;
using IdCards = Dictionary<string, GxtIdentity>;

public static class ModInfo
{
    public const string Guid = "iah.gxt.IdCard";
    public const string Name = "GXT ID Card";
    public const string Version = "1.0.0";
}

public class ElinIdCardMeta
{
    public string CharacterName { get; }
    public string SteamName { get; }
    public string Portrait { get; }
    public int CurrentLevel { get; }
    public string Race { get; }

    public ElinIdCardMeta(string characterName, string steamName, string portrait, int currentLevel, string race)
    {
        CharacterName = characterName;
        SteamName = steamName;
        Portrait = portrait;
        CurrentLevel = currentLevel;
        Race = race;
    }

    public string FullName()
    {
        return $"{CharacterName} ({SteamName})";
    }
}

class IdCardNote : Thing
{
    ElinIdCardMeta _Meta;
    public IdCardNote(ElinIdCardMeta meta) { _Meta = meta; }

    public override string GetName(NameStyle style, int num = -1) { return "ID Card"; }

    public override void WriteNote(UINote n, Action<UINote>? onWriteNote = null, IInspect.NoteMode mode = IInspect.NoteMode.Default, Recipe? recipe = null)
    {
        n.Clear();
        n.AddHeader($"Player Name: {_Meta.SteamName}");
        n.AddText($"Character Name: {_Meta.CharacterName}");
        n.AddText($"Level: {_Meta.CurrentLevel}");
        n.AddText($"Race: {_Meta.Race}");

        var portrait = Portrait.modPortraits.dict[_Meta.Portrait];
        n.AddImage(portrait.GetObject());
    }
}

class AddressBookContactsTab : YKLayout<IdCards>
{
    private int _Current = 0;
    private List<GxtIdentity> _Values = new();
    private List<string> CreateOptions()
    {
        return _Values.Select(id => id.Meta.FullName()).ToList();
    }
    public override void OnLayout()
    {
        _Values = [.. Layer.Data.Values];
        var d = Dropdown(CreateOptions(), i => _Current = i, _Current);
        var h = Horizontal();
        h.Button("Open", () =>
        {
            SE.Rotate();
            EClass.ui.AddLayer<LayerInfo>().Set(new IdCardNote(_Values[_Current].Meta));
        });

        h.Button("My ID Card", () =>
        {
            SE.Rotate();
            EClass.ui.AddLayer<LayerInfo>().Set(new IdCardNote(Plugin.IdCardMeta()));
        });

        h.Button("Delete", () =>
        {
            var id = _Values[_Current].VerificationKey;
            Plugin.Contacts.Remove(id);
            _Values.RemoveAll(identity => identity.VerificationKey == id);
            d.options = CreateOptions().ToDropdownOptions();
        });
    }
}

class AddressBook : YKLayer<IdCards>
{
    public override void OnLayout()
    {
        CreateTab<AddressBookContactsTab>("Contacts", "");
    }
}

public class GxtIdentity
{
    public string VerificationKey { get; set; }
    public string EncryptionKey { get; set; }
    public ElinIdCardMeta Meta { get; set; }
    public GxtIdentity(Envelope<ElinIdCardMeta> envelope)
    {
        VerificationKey = envelope.verification_key;
        EncryptionKey = envelope.encryption_key;
        Meta = envelope.payload;
    }
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class Plugin : BaseUnityPlugin
{
    internal static Plugin? Instance;
    public static Dictionary<string, GxtIdentity> Contacts = new Dictionary<string, GxtIdentity>();

    const string PRIVATE_KEY_CHUNK = "Elin.Gxt.PrivateKey";
    const string ADDRESS_BOOK_CHUNK = "Elin.Gxt.AddressBook";
    static string PrivateKey = GxtSdk.MakeKey();

    [CwlPreSave]
    static void MySaveHandler(GameIOProcessor.GameIOContext context)
    {
        if (string.IsNullOrWhiteSpace(PrivateKey))
        {
            PrivateKey = GxtSdk.MakeKey();
        }
        context.Save(PrivateKey, PRIVATE_KEY_CHUNK);

        if (Plugin.Contacts is null)
        {
            Plugin.Contacts = new Dictionary<string, GxtIdentity>();
        }
        context.Save(Plugin.Contacts, ADDRESS_BOOK_CHUNK);
    }

    [CwlPostLoad]
    static void MyLoadHandler(GameIOProcessor.GameIOContext context)
    {
        if (!context.Load<string>(out PrivateKey!, PRIVATE_KEY_CHUNK) || PrivateKey is null || string.IsNullOrWhiteSpace(PrivateKey))
        {
            PrivateKey = GxtSdk.MakeKey();
        }

        if (!context.Load<Dictionary<string, GxtIdentity>>(out Plugin.Contacts!, ADDRESS_BOOK_CHUNK) || Plugin.Contacts is null)
        {
            Plugin.Contacts = new Dictionary<string, GxtIdentity>();
        }
    }


    private static string GetSteamName()
    {
        if (Steam.Instance == null)
        {
            return "N/A";
        }

        return SteamFriends.GetPersonaName();
    }

    public static ElinIdCardMeta IdCardMeta()
    {
        return new ElinIdCardMeta(
             Game.pc.NameSimple,
             GetSteamName(),
             Game.pc.GetIdPortrait(),
             Game.pc.LV,
             Game.pc.race.GetName()
        );
    }


    internal static string MakeIdCard()
    {
        return GxtSdk.MakeIdCard(PrivateKey, IdCardMeta());
    }

    [CwlContextMenu("GXT/gxt_ui_address_book_show")]
    internal static void ShowAddressBook()
    {
        SE.Rotate();
        YK.CreateLayer<AddressBook, IdCards>(Plugin.Contacts);
    }

    [CwlContextMenu("GXT/gxt_ui_id_card_copy")]
    internal static void CopyIdToClipboard()
    {
        GUIUtility.systemCopyBuffer = MakeIdCard();
        Say("ID Card copied to Clipboard");
    }

    [CwlContextMenu("GXT/gxt_ui_id_card_import")]
    internal static void ContextMenuImportFromClipboard()
    {
        try
        {
            ImportFromClipboard();
        }
        catch (Exception e)
        {
            Say("Failed to import identity. Make sure you have an ID Card token is in your clipboard!");
            LogError($"Failed to import identity: {e.Message}");
        }
    }

    internal static void ImportFromClipboard(Action<GxtIdentity>? onNewIdentity = null)
    {
        var id_card_token = GUIUtility.systemCopyBuffer;
        var envelope = GxtSdk.VerifyMessage<ElinIdCardMeta>(id_card_token);
        var id = envelope.verification_key;
        if (Contacts.ContainsKey(id))
        {
            Contacts[id].Meta = envelope.payload;
            Contacts[id].EncryptionKey = envelope.encryption_key;
            return;
        }
        try
        {
            var identity = new GxtIdentity(envelope);
            if (onNewIdentity is not null)
            {
                onNewIdentity(identity);
            }
            Contacts.Add(id, identity);
        }
        catch (FfiException e)
        {
            Say($"Failed to import the ID Card. Error: {e.ErrorCode}");
        }
        catch (Exception e)
        {
            Say($"Failed to import the ID Card. {e.Message}");
        }
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