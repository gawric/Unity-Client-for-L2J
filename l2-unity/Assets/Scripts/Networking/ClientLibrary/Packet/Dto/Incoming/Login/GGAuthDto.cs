using UnityEngine;

public sealed class GGAuthDto : IWireDto
{
    public int Response;

    public void ReadFrom(PacketReader reader)
    {
        Response = reader.ReadI();
        reader.ReadI();
        reader.ReadI();
        reader.ReadI();
        reader.ReadI();
        Debug.Log("GGAuth response " + Response);
    }
}
