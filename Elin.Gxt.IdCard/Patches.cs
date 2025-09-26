using HarmonyLib;
using Cwl.API.Processors;
using Cwl.API.Attributes;
using Elin.Gxt;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace ElinGxtIdCard.Patches;
#pragma warning restore IDE0130 // Namespace does not match folder structure

internal static class GamePatches
{
    const string PRIVATE_KEY_CHUNK = "Elin.Gxt.PrivateKey";
    const string PRIVATE_ID_CHUNK = "Elin.Gxt.ID";
    const string ADDRESS_BOOK_CHUNK = "Elin.Gxt.AddressBook";

    [HarmonyPrefix, HarmonyPatch(typeof(Game), nameof(Game.GotoTitle))]
    public static void GotoTitlePatch()
    {
        AddressBook.IsLoaded = false;
    }

    [CwlPreSave]
    static void MySaveHandler(GameIOProcessor.GameIOContext context)
    {
        if (string.IsNullOrWhiteSpace(AddressBook.PrivateKey))
        {
            AddressBook.PrivateKey = GxtSdk.MakeKey();
        }
        context.Save(AddressBook.PrivateKey, PRIVATE_KEY_CHUNK);

        if (string.IsNullOrWhiteSpace(AddressBook.Id))
        {
            var envelope = GxtSdk.VerifyMessage<ContactInfo>(AddressBook.GetIdCardToken()!);
            AddressBook.Id = envelope.verification_key;
        }
        context.Save(AddressBook.Id, PRIVATE_ID_CHUNK);

        AddressBook.Contacts ??= [];
        context.Save(AddressBook.Contacts, ADDRESS_BOOK_CHUNK);
    }

    [CwlPostLoad]
    static void MyLoadHandler(GameIOProcessor.GameIOContext context)
    {
        AddressBook.IsLoaded = true;
        if (!context.Load(out AddressBook.PrivateKey!, PRIVATE_KEY_CHUNK) || AddressBook.PrivateKey is null || string.IsNullOrWhiteSpace(AddressBook.PrivateKey))
        {
            AddressBook.PrivateKey = GxtSdk.MakeKey();
        }

        if (!context.Load(out string? id, PRIVATE_ID_CHUNK) || id is null || string.IsNullOrWhiteSpace(id))
        {
            var envelope = GxtSdk.VerifyMessage<ContactInfo>(AddressBook.GetIdCardToken()!);
            id = envelope.verification_key;
        }
        AddressBook.Id = id;

        if (!context.Load(out Identities? contacts, ADDRESS_BOOK_CHUNK) || contacts is null)
        {
            contacts = [];
        }
        AddressBook.Contacts = contacts;
    }
}
