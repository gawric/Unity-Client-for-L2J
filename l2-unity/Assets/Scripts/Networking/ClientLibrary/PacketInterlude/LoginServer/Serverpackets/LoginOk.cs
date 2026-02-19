using UnityEngine;

public class LoginOk : ServerPacket
{
    //server model
    //buffer.writeByte(0x03);
    //buffer.writeInt(_loginOk1);
    //buffer.writeInt(_loginOk2);
    //buffer.writeInt(0x00);
    //buffer.writeInt(0x00);
    //buffer.writeInt(0x000003ea);
    //buffer.writeInt(0x00);
    //buffer.writeInt(0x00);
    //buffer.writeInt(0x00);
    //buffer.writeBytes(new byte[16]);

    private int _sessionKey1;
    private int _sessionKey2;
    public int SessionKey1 { get { return _sessionKey1; } }
    public int SessionKey2 { get { return _sessionKey2; } }

    public LoginOk(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        _sessionKey1 = ReadI();
        _sessionKey2 = ReadI();

        Debug.Log("session key 1 " + _sessionKey1);
        Debug.Log("session key 2 " + _sessionKey2);
    }
}
