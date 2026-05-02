using UnityEngine;

public class SocialAction : ServerPacket
{
    private int _objectId;
    private int _actionId;

    public int ObjectId { get => _objectId; }
    public int ActionId { get => _actionId; }
    public SocialAction(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        _objectId = ReadI();
        _actionId = ReadI();
    }
}
