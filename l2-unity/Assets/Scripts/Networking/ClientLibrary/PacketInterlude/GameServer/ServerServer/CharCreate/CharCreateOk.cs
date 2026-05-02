using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharCreateOk : ServerPacket
{
    private bool _isCreate = false;
    public bool IsCreate { get { return _isCreate; } }
    public CharCreateOk(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        _isCreate = ReadI() == 1;
    }
}
