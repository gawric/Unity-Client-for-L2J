using System.Security.Principal;
using UnityEngine;

public class Revive : ServerPacket
{
    private int _objectId;

    public int ObjectId { get => _objectId; }
    public Revive(byte[] d) : base(d)
    {

        Parse();
    }

    public override void Parse()
    {
        _objectId = ReadI();

    }
}
