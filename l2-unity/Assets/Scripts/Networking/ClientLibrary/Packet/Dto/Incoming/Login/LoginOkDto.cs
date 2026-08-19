using UnityEngine;

public sealed class LoginOkDto : IWireDto
{
    public int SessionKey1;
    public int SessionKey2;

    public void ReadFrom(PacketReader reader)
    {
        SessionKey1 = reader.ReadI();
        SessionKey2 = reader.ReadI();
        Debug.Log("session key 1 " + SessionKey1);
        Debug.Log("session key 2 " + SessionKey2);
    }
}
