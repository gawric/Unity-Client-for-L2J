using UnityEngine;

/// <summary>
/// An item that is already lying on the ground entering the player's visibility range - either on
/// login/zone entry or by walking closer to it later. Same information as DropItem, minus the
/// dropper's object id (nobody just dropped it, it was already there).
///
/// Wire format (org.l2jmobius.gameserver.network.serverpackets.SpawnItem#writeImpl):
///   objectId, displayId, x, y, z, isStackable, count, 0 (c2 padding)
/// </summary>
public class SpawnItemDto : IWireDto
{
    /// <summary>World id of the item on the ground, unique per drop.</summary>
    public int ItemObjectId { get; private set; }

    /// <summary>Item template id, the one the dat tables are keyed by (displayId on the server).</summary>
    public int ItemId { get; private set; }

    public Vector3 Position { get; private set; }

    public int Count { get; private set; }

    public bool Stackable { get; private set; }

    public void ReadFrom(PacketReader reader)
    {
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
