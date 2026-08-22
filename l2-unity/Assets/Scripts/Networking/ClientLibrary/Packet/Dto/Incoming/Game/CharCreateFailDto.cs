using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharCreateFailDto : IWireDto
{
    private string  _text = "";
    public string Text { get { return _text; } }
    

    public void ReadFrom(PacketReader reader)
    {
        int id = reader.ReadI();
        _text = ErrorType.GetErrorText(id);
    }
}
