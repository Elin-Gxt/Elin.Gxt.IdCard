using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Cwl.API.Attributes;
using ElinGxtIdCard;
using UnityEngine;
using YKF;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace ElinGxtMessage.Ui;
#pragma warning restore IDE0130 // Namespace does not match folder structure

class MessageNote : Thing
{
    private readonly Msg _Msg;
    internal MessageNote(Msg msg)
    {
        _Msg = msg;
    }

    public override string GetName(NameStyle style, int num = -1) { return $"From: {_Msg.Sender}"; }

    public override void WriteNote(UINote n, Action<UINote>? onWriteNote = null, IInspect.NoteMode mode = IInspect.NoteMode.Default, Recipe? recipe = null)
    {
        n.Clear();
        n.AddText(_Msg.Text);
    }
}

class Msg
{
    public string Sender;
    public string Text;
}

class CreateMessageTab : YKLayout<List<GxtIdentity>>
{
    public override void OnLayout()
    {
        var selectedIndex = 0;
        Clear();
        Dropdown([.. Layer.Data.Select(i => i.Meta.FullName)], i => selectedIndex = i, selectedIndex);
        var t = Text("");
        var h = Horizontal();
        h.Button("Edit Message", () =>
        {
            var tmp = Path.GetTempFileName();
            var p = Process.Start("notepad.exe", tmp);
            p.Exited += (sender, e) =>
            {
                t.text = File.ReadAllText(tmp);
                File.Delete(tmp);
            };
        });
        Button("Send", () =>
        {
            GUIUtility.systemCopyBuffer = Message.Encrypt(
                Layer.Data[selectedIndex].IdCard,
                new Msg
                {
                    Sender = AddressBook.GetOwnContactInfo()!.FullName,
                    Text = t.text
                });
            Layer.Close();
        });
    }
}

class CreateMessageLayer : YKLayer<List<GxtIdentity>>
{
    public override void OnLayout()
    {
        CreateTab<CreateMessageTab>("", "Create Message");
    }
}

static class ContextMenu
{
    [CwlContextMenu("GXT/gxt_ui_msg_export")]
    internal static void ExportToClipboard()
    {
        YK.CreateLayer<CreateMessageLayer, List<GxtIdentity>>(AddressBook.GetContacts()!.Values.ToList());
    }

    [CwlContextMenu("GXT/gxt_ui_msg_import")]
    internal static void ContextMenuImportFromClipboard()
    {
        var msg_token = GUIUtility.systemCopyBuffer;
        var msg = Message.Decrypt<Msg>(msg_token);

        SE.Rotate();
        EClass.ui.AddLayer<LayerInfo>().Set(new MessageNote(msg));
    }
}
