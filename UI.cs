using System;
using System.Collections.Generic;
using System.Linq;
using YKF;
using Cwl.API.Attributes;
using UnityEngine;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace ElinGxtIdCard.Ui;
#pragma warning restore IDE0130 // Namespace does not match folder structure

internal static class UiHelpers
{
    internal static void Say(string text)
    {
        Msg.SayGod(text);
    }
}

class IdCardNote : Thing
{
    private readonly ContactInfo _Meta;
    internal IdCardNote(ContactInfo meta) { _Meta = meta; }

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

class AddressBookContactsTab : YKLayout<Identities>
{
    private int _Current = 0;
    private List<GxtIdentity> _Values = [];
    private List<string> CreateOptions()
    {
        return [.. _Values.Select(id => id.Meta.FullName)];
    }
    public override void OnLayout()
    {
        _Values = [.. Layer.Data.Values];
        var d = Dropdown(CreateOptions(), i => _Current = i, _Current);
        var h1 = Horizontal();
        h1.Button("Open", () =>
        {
            SE.Rotate();
            EClass.ui.AddLayer<LayerInfo>().Set(new IdCardNote(_Values[_Current].Meta));
        });

        h1.Button("My ID Card", () =>
        {
            SE.Rotate();
            EClass.ui.AddLayer<LayerInfo>().Set(new IdCardNote(AddressBook.GetOwnContactInfo()!));
        }).WithMinWidth(110);

        h1.Button("Delete", () =>
        {
            var id = _Values[_Current].VerificationKey;
            AddressBook.Contacts!.Remove(id);
            _Values.RemoveAll(identity => identity.VerificationKey == id);
            d.options = CreateOptions().ToDropdownOptions();
        });

        var h2 = Horizontal();
        h2.Button("Import from Clipboard", () =>
        {
            AddressBook.ImportFromClipboard(id =>
            {
                _Values.Add(id);
                d.options = CreateOptions().ToDropdownOptions();
            });
        }).WithMinWidth(180);

        h2.Button("Export your ID Card", AddressBook.ExportToClipboard).WithMinWidth(180);
    }
}

class AddressBookLayer : YKLayer<Identities>
{
    public override void OnLayout()
    {
        CreateTab<AddressBookContactsTab>("Contacts", "");
    }
}

static class ContextMenu
{
    [CwlContextMenu("GXT/gxt_ui_address_book_show")]
    internal static void ShowAddressBook()
    {
        SE.Rotate();
        YK.CreateLayer<AddressBookLayer, Identities>(AddressBook.Contacts!);
    }

    [CwlContextMenu("GXT/gxt_ui_id_card_copy")]
    internal static void CopyIdToClipboard()
    {
        AddressBook.ExportToClipboard();
    }

    [CwlContextMenu("GXT/gxt_ui_id_card_import")]
    internal static void ContextMenuImportFromClipboard()
    {
        try
        {
            AddressBook.ImportFromClipboard();
        }
        catch (Exception e)
        {
            UiHelpers.Say("Failed to import identity. Make sure you have an ID Card token is in your clipboard!");
            Logger.Error($"Failed to import identity: {e.Message}");
        }
    }
}
