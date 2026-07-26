using UnityEngine;

/// <summary>
/// An item that is already lying on the ground entering the player's visibility range - either on
/// login/zone entry or by walking closer to it later. Same information as DropItem, minus the
/// dropper's object id (nobody just dropped it, it was already there).
///
/// Wire format (org.l2jmobius.gameserver.network.serverpackets.SpawnItem#writeImpl):
///   objectId, displayId, x, y, z, isStackable, count, 0 (c2 padding)
/// </summary>
public class SpawnItem : ServerPacket
{
    private int _itemObjectId;
    /// <summary>
    /// Мировой id предмета на земле, уникален для каждого дропа.
    /// </summary>
    public int ItemObjectId { get => _itemObjectId; }

    private int _itemId;
    /// <summary>
    /// Id предмета из dat-таблиц (displayId на сервере).
    /// </summary>
    public int ItemId { get => _itemId; }

    public Vector3 Coordinats { get; set; }

    private int _count;
    public int Count { get => _count; }

    public bool Stackable { get; private set; }

    public SpawnItem(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        _itemObjectId = ReadI();
        _itemId = ReadI();
        var x = ReadI();
        var y = ReadI();
        var z = ReadI();
        Coordinats = VectorUtils.ConvertPosToUnity(new Vector3(x, y, z));
        Stackable = ReadI() != 0;
        _count = ReadI();
        var unknown = ReadI();
    }
}
