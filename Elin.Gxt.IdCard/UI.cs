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

class ConfirmationData
{
    public string? Question;
    public string ConfirmText = "Yes";
    public string AbortText = "No";
    public Action<bool>? Callback;
}

class ConfirmationDialogTab : YKLayout<ConfirmationData>
{
    public override void OnLayout()
    {
        Clear();
        var cd = Layer.Data;
        Header(cd.Question!);
        var h = Horizontal();
        h.Button(cd.ConfirmText, () =>
        {
            cd.Callback!(true);
            Layer.Close();
        });
        h.Button(cd.AbortText, () =>
        {
            cd.Callback!(false);
            Layer.Close();
        });
    }
}

class ConfirmationDialog : YKLayer<ConfirmationData>
{
    public override void OnLayout()
    {
        CreateTab<ConfirmationDialogTab>("", "");
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
    public void OpenCurrentIdCard()
    {
        SE.Rotate();
        EClass.ui.AddLayer<LayerInfo>().Set(new IdCardNote(_Values[_Current].Meta));
    }
    public override void OnLayout()
    {
        _Values = [.. Layer.Data.Values];
        Clear();

        var d = Dropdown(CreateOptions(), i => _Current = i, _Current);
        d.enabled = _Values.Count > 0;

        var h1 = Horizontal();
        var o = h1.Button("Open", OpenCurrentIdCard);
        d.enabled = _Values.Count > 0;

        UIButton? delete_button = null;
        delete_button = h1.Button("Delete", () =>
        {
            var id = _Values[_Current].VerificationKey;
            AddressBook.Contacts!.Remove(id);
            _Values.RemoveAll(identity => identity.VerificationKey == id);
            d.options = CreateOptions().ToDropdownOptions();
            d.enabled = _Values.Count > 0;
            delete_button!.enabled = _Values.Count > 0;
            o.enabled = _Values.Count > 0;
        });
        delete_button.enabled = _Values.Count > 0;

        var h2 = Horizontal();
        h2.Button("Import from Clipboard", () =>
        {
            AddressBook.ImportFromClipboard(id =>
            {
                _Values.Add(id);
                d.options = CreateOptions().ToDropdownOptions();
                _Current = _Values.Count - 1;
                d.value = _Current;
                d.enabled = true;
                delete_button!.enabled = true;
                o.enabled = _Values.Count > 0;
                SE.Rotate();
                YK.CreateLayer<ConfirmationDialog, ConfirmationData>(new ConfirmationData
                {
                    Question = "ID Card imported successfully. Do you want to open it?",
                    Callback = (c) =>
                    {
                        if (c)
                        {
                            SE.Rotate();
                            EClass.ui.AddLayer<LayerInfo>().Set(new IdCardNote(id.Meta));
                        }
                    }
                });
            });
        }).WithMinWidth(180);

        h2.Button("Export your ID Card", AddressBook.ExportIdToClipboard).WithMinWidth(180);

        Button("My ID Card", () =>
        {
            SE.Rotate();
            EClass.ui.AddLayer<LayerInfo>().Set(new IdCardNote(AddressBook.GetOwnContactInfo()!));
        }).WithMinWidth(110);
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

#if DEBUG
    [CwlContextMenu("GXT/gxt_ui_key_export")]
    internal static void ExportKeyToClipboard()
    {
        AddressBook.ExportKeyToClipboard();
    }

    [CwlContextMenu("GXT/gxt_ui_id_card_export")]
    internal static void ExportIdToClipboard()
    {
        AddressBook.ExportIdToClipboard();
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
#endif
}
