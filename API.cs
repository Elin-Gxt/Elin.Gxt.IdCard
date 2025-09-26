using System;
using System.Collections.Immutable;
using Elin.Gxt;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using ElinGxtIdCard.Ui;
using Newtonsoft.Json;

namespace ElinGxtIdCard;

using ImmutableIdentities = ImmutableDictionary<string, GxtIdentity>;
public class Identities : Dictionary<string, GxtIdentity>;

public class ContactInfo
{
    [JsonProperty]
    public string CharacterName { get; private set; }
    [JsonProperty]
    public string SteamName { get; private set; }
    [JsonProperty]
    public string Portrait { get; private set; }
    [JsonProperty]
    public int CurrentLevel { get; private set; }
    [JsonProperty]
    public string Race { get; private set; }

    internal ContactInfo(string characterName, string steamName, string portrait, int currentLevel, string race)
    {
        CharacterName = characterName;
        SteamName = steamName;
        Portrait = portrait;
        CurrentLevel = currentLevel;
        Race = race;
    }

    public string FullName
    {
        get
        {
            return $"{CharacterName} ({SteamName})";
        }
    }

    public ContactInfo Clone()
    {
        return new ContactInfo
        (
             (string)CharacterName.Clone(),
             (string)SteamName.Clone(),
             (string)Portrait.Clone(),
             CurrentLevel,
             (string)Race.Clone()
        );
    }
}
public class GxtIdentity
{
    [JsonProperty]
    public string VerificationKey { get; internal set; }
    [JsonProperty]
    public string EncryptionKey { get; internal set; }
    [JsonProperty]
    public ContactInfo Meta { get; internal set; }
    internal GxtIdentity(Envelope<ContactInfo> envelope)
    {
        VerificationKey = envelope.verification_key;
        EncryptionKey = envelope.encryption_key;
        Meta = envelope.payload;
    }

    internal GxtIdentity(string verificationKey, string encryptionKey, ContactInfo meta)
    {
        VerificationKey = verificationKey;
        EncryptionKey = encryptionKey;
        Meta = meta;
    }

    public GxtIdentity Clone()
    {
        return new GxtIdentity(
             (string)VerificationKey.Clone(),
             (string)EncryptionKey.Clone(),
             Meta.Clone()
        );
    }
}

public static class AddressBook
{
    public static bool IsLoaded { get; internal set; } = false;
    public static ImmutableIdentities? GetContacts()
    {
        return IsLoaded ? Contacts!.ToImmutableDictionary() : null;
    }

    public static ContactInfo? GetOwnContactInfo()
    {
        return IsLoaded ? new ContactInfo(
             Game.pc.NameSimple,
             GetSteamName(),
             Game.pc.GetIdPortrait(),
             Game.pc.LV,
             Game.pc.race.GetName()
        ) : null;
    }

    [JsonProperty]
    internal static Identities? Contacts { get; set; }
    [JsonProperty]
    internal static string? PrivateKey;

    internal static string? GetIdCardToken()
    {
        return IsLoaded ? GxtSdk.MakeIdCard(PrivateKey!, GetOwnContactInfo()) : null;
    }

    internal static void ImportFromClipboard(Action<GxtIdentity>? onNewIdentity = null)
    {
        if (!IsLoaded)
        {
            Logger.Error("No game loaded, can't import id card.");
            return;
        }

        var id_card_token = GUIUtility.systemCopyBuffer;
        var envelope = GxtSdk.VerifyMessage<ContactInfo>(id_card_token);
        var id = envelope.verification_key;
        if (Contacts!.ContainsKey(id))
        {
            Contacts[id].Meta = envelope.payload;
            Contacts[id].EncryptionKey = envelope.encryption_key;
            UiHelpers.Say($"Updated existing ID Card: {envelope.payload.FullName}");
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
            UiHelpers.Say("ID Card imported successfully.");
        }
        catch (FfiException e)
        {
            UiHelpers.Say("Failed to import the ID Card.");
            Logger.Error($"Failed to import the ID Card. Error: {e.ErrorCode}");
        }
        catch (Exception e)
        {
            UiHelpers.Say("Failed to import the ID Card.");
            Logger.Error($"Failed to import the ID Card. {e.Message}");
        }
    }

    internal static string GetSteamName()
    {
        if (Steam.Instance == null)
        {
            return "N/A";
        }

        return SteamFriends.GetPersonaName();
    }
}
