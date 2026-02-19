using System;
using UnityEngine;

public class DeleteObject : ServerPacket
{
    private int _objectId;

    public int ObjectId { get => _objectId; }
    public DeleteObject(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        _objectId = ReadI();
         ReadI(); // c2
    }
}
