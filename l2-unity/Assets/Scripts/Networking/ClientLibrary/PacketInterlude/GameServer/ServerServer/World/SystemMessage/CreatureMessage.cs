using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class CreatureMessage : SystemMessage
{
    private string sendName;
    private string _text;
    public ChatTypeData _data;
    public CreatureMessage(string sendName , string text , ChatTypeData data) : base(null, null)
    {
        this.sendName = sendName;
        _text = text;
        _data = data;
    }

    public override string ToString()
    {
        return "<color="+_data.Color+">" + sendName+": "+ _text+ "</color>";
    }
}
