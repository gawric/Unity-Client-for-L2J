
using UnityEngine;

public class GetItem : ServerPacket
{
    private int _playerId;
    public int PlayerId { get => _playerId; }
    //private ItemInstance _item;
    //public ItemInstance Item { get => _item; }

    private int _objectId { get; set; }

    public int ObjectId { get => _objectId; }

    private Vector3 _coordinates { get; set; }

    public Vector3 Coordinates { get => _coordinates; }

    public GetItem(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        _playerId = ReadI();
        var objectId = ReadI();
        _objectId = objectId;
        var x = ReadI();
        var y = ReadI();
        var z = ReadI();
        _coordinates = new Vector3(x, y, z);
    }
}
