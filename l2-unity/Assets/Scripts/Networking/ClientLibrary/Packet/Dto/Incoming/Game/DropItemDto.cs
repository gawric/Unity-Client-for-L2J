using UnityEngine;

/// <summary>
/// Another player/creature dropped an item on the ground.
/// Field order mirrors org.l2jmobius...serverpackets.DropItem#writeImpl.
/// </summary>
public class DropItemDto : IWireDto
{
    /// <summary>Object id of whoever dropped the item (not used for display, kept for parity).</summary>
    public int ObjectId { get; private set; }

    /// <summary>World id of the dropped item itself, unique per drop - not the item template id.</summary>
    public int ItemObjectId { get; private set; }

    /// <summary>Item template id, the one the dat tables are keyed by.</summary>
    public int ItemId { get; private set; }

    public Vector3 Position { get; private set; }

    public int Count { get; private set; }

    public bool Stackable { get; private set; }

    public void ReadFrom(PacketReader reader)
    {
        ObjectId = reader.ReadI();
        ItemObjectId = reader.ReadI();
        ItemId = reader.ReadI();
        int x = reader.ReadI();
        int y = reader.ReadI();
        int z = reader.ReadI();
        Position = VectorUtils.ConvertPosToUnity(new Vector3(x, y, z));
        Stackable = reader.ReadI() != 0;
        Count = reader.ReadI();
        reader.ReadI(); // unused trailing field
    }
}
