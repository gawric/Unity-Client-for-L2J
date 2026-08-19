using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharCreateOkDto : IWireDto
{
    private bool _isCreate = false;
    public bool IsCreate { get { return _isCreate; } }
    

    public void ReadFrom(PacketReader reader)
    {
        _isCreate = reader.ReadI() == 1;
    }
}
