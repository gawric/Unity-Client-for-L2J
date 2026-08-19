using System;

public class ItemServer
{
    private readonly byte[] _data;
    private readonly byte _byteType;
    private readonly GameServerPacketType packetType;
    private int _exByteType;

    public ItemServer(byte[] data)
    {
        _data = data;
        _byteType = data[0];
        packetType = (GameServerPacketType)data[0];
    }

    public byte[] DecodeData() { return _data; }

    public byte[] DecodeExData()
    {
        return Delete2And3Byte(_data);
    }

    public byte ByteType() { return _byteType; }
    public GameServerPacketType PaketType() { return packetType; }

    public int ExPacketType()
    {
        _exByteType = ReadSh(_data);
        return _exByteType;
    }

    protected int ReadSh(byte[] packetData)
    {
        byte[] data = new byte[2];
        Array.Copy(packetData, 1, data, 0, 2);
        Array.Reverse(data);
        short value = ByteUtils.fromByteArrayShort(data);
        return value;
    }

    private byte[] Delete2And3Byte(byte[] data)
    {
        byte[] newData = new byte[data.Length - 2];
        int pos = 0;
        Array.Copy(data, 0, newData, pos, 1);
        pos += 1;
        Array.Copy(_data, 3, newData, pos, data.Length - 3);
        return newData;
    }
}
