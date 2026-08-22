using UnityEngine;

/// <summary>Another player/creature picked an item up off the ground.</summary>
public class GetItemDto : IWireDto
{
    /// <summary>Object id of whoever picked the item up.</summary>
    public int PlayerId { get; private set; }

    /// <summary>World id of the dropped item that was picked up.</summary>
    public int ObjectId { get; private set; }

    public Vector3 Position { get; private set; }

    public void ReadFrom(PacketReader reader)
    {
        PlayerId = reader.ReadI();
        ObjectId = reader.ReadI();
        int x = reader.ReadI();
        int y = reader.ReadI();
        int z = reader.ReadI();
        Position = VectorUtils.ConvertPosToUnity(new Vector3(x, y, z));
    }
}
