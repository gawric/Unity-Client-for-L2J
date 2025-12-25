using UnityEngine;

public class DropItem : ServerPacket
{
    private int _objectId;
    /// <summary>
    /// playerid
    /// </summary>
    public int ObjectId { get => _objectId; }

    //private ItemInstance _item;
    //public ItemInstance Item { get => _item; } //todo: думаю должна быть другая структура

    public Vector3 Coordinats { get; set; }

    private int _itemId { get; set; }
    public int ItemId { get => _itemId; }

    private int _displayId { get; set; }
    public int DisplayId { get => _displayId; }

    private int _count { get; set; }

    public int Count { get => _count; }

    public bool Stackable { get; private set; }

    public DropItem(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        _objectId = ReadI();
        var itemId = ReadI();
        _itemId = itemId;
        var displayId = ReadI();
        _displayId = displayId;
        var x = ReadI();
        var y = ReadI();
        var z = ReadI();
        var vector = new Vector3(x, y, z);
        Coordinats = VectorUtils.ConvertPosToUnity(vector);
        var isStackable = ReadI();
        Stackable = isStackable != 0;
        var count = ReadI();
        _count = count;
        var unknown = ReadI();
    }
}
