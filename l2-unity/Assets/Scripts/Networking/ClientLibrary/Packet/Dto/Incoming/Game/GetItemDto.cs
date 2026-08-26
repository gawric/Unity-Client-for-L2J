using UnityEngine;

/// <summary>aCis GetItem 0x0D: playerId, objectId, x,y,z.</summary>
public sealed class GetItemDto : IWireDto
{
    public int PlayerId { get; private set; }
    public int ObjectId { get; private set; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Z { get; private set; }
    public Vector3 UnityPos { get; private set; }

    public void ReadFrom(PacketReader reader)
    {
        PlayerId = reader.ReadI();
        ObjectId = reader.ReadI();
        X = reader.ReadI();
        Y = reader.ReadI();
        Z = reader.ReadI();
        UnityPos = VectorUtils.ConvertPosToUnity(new Vector3(X, Y, Z));
    }
}
