using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharCreateFail : ServerPacket
{
    private string  _text = "";
    public string Text { get { return _text; } }
    public CharCreateFail(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        int id = ReadI();
        _text = ErrorType.GetErrorText(id);
    }
}
