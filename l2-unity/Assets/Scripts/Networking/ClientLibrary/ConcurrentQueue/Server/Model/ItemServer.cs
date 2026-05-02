using L2_login;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ItemServer :  IData
{
    private byte[] _data;
    private GameInterludeServerPacketType packetType;
    private byte _byteType;
    private int _exByteType;
 
    public ItemServer(byte[] data, bool init, bool cryptEnbled)
    {

        Decrypt(data, cryptEnbled, init);
        packetType = (GameInterludeServerPacketType)data[0];
        _byteType = data[0];
        _data = data;
    }

    public byte[] DecodeData() { return _data; }
    //remove 3 bytes = 1 byte: id ExPacket & 2 byte id packet(type short)
    public byte[] DecodeExData() {
        return Delete2And3Byte(_data);
    }
    public byte ByteType() { return _byteType; }
    public GameInterludeServerPacketType PaketType() { return packetType; }

    public int ExPacketType() {

        _exByteType = ReadSh(_data);
        return _exByteType;
    }

    private void Decrypt(byte[] data, bool cryptEnbled, bool init)
    {
        if (cryptEnbled)
        {
            GameClient.Instance.GameCrypt.GameServerDecrypt(data, 0, data.Length);
        }

    }

  

    public ClientPacket GetPacket()
    {
        throw new System.NotImplementedException();
    }

    public string ToString()
    {
        return "ItemServer{" +
            ", packetType=" + packetType +
            ", byteType=" + _byteType +
        '}';
    }

    protected int ReadSh(byte[] packetData)
    {
        byte[] data = new byte[2];
        Array.Copy(packetData, 1, data, 0, 2);
        Array.Reverse(data);
        short value = ByteUtils.fromByteArrayShort(data);
        return value;
    }
    //l2j Server Ex  use Short type
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
