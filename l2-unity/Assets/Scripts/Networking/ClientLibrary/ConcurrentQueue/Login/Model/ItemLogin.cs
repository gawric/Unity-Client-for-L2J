public class ItemLogin
{
    private readonly byte[] _data;
    private readonly LoginServerPacketType packetType;

    public ItemLogin(byte[] data)
    {
        packetType = (LoginServerPacketType)data[0];
        _data = data;
    }

    public byte[] DecodeData() { return _data; }
    public LoginServerPacketType PaketType() { return packetType; }
}
