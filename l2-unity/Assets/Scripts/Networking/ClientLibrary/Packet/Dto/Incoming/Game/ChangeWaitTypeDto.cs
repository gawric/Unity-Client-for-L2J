using UnityEngine;

public sealed class ChangeWaitTypeDto : IWireDto
{
    public int ObjectId { get; private set; }
    public WaitType WaitType { get; private set; }
    public Vector3 Position { get; private set; }

    public void ReadFrom(PacketReader reader)
    {
        ObjectId = reader.ReadI();
        WaitType = (WaitType)reader.ReadI();
        int x = reader.ReadI();
        int y = reader.ReadI();
        int z = reader.ReadI();
        Position = VectorUtils.ConvertPosToUnity(new Vector3(x, y, z));
    }
}
