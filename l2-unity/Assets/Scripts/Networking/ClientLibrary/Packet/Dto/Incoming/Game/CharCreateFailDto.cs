using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharCreateFailDto : IWireDto
{
    private string  _text = "";
    public string Text { get { return _text; } }

    public static CharCreateFailDto From(int reason)
    {
        return new CharCreateFailDto { _text = ErrorType.GetErrorText(reason) };
    }

    public void ReadFrom(PacketReader reader)
    {
        int id = reader.ReadI();
        _text = ErrorType.GetErrorText(id);
    }
}
