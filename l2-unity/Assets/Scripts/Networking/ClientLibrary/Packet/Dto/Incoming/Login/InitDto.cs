using UnityEngine;

public sealed class InitDto : IWireDto
{
    public int SessionId;
    public int Protocol;
    public byte[] PublicKey;
    public int Gg1;
    public int Gg2;
    public int Gg3;
    public int Gg4;
    public byte[] BlowfishKey;

    public int[] GG
    {
        get { return new int[4] { Gg1, Gg2, Gg3, Gg4 }; }
    }

    public void ReadFrom(PacketReader reader)
    {
        SessionId = reader.ReadI();
        Protocol = reader.ReadI();
        PublicKey = reader.ReadB(128);
        Debug.Log("InitPacket Publick KEY: " + StringUtils.ByteArrayToString(PublicKey));
        Gg1 = reader.ReadI();
        Gg2 = reader.ReadI();
        Gg3 = reader.ReadI();
        Gg4 = reader.ReadI();
        BlowfishKey = reader.ReadB(16);
    }
}
