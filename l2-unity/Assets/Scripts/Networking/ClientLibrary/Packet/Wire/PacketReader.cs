using System;
using System.Text;
using UnityEngine;

public sealed class PacketReader
{
    private readonly byte[] _data;
    private int _iterator;

    public PacketReader(byte[] data, bool skipOpcode = true)
    {
        _data = data;
        if (skipOpcode && _data != null && _data.Length > 0)
            ReadB();
    }

    public int Remaining
    {
        get { return _data == null ? 0 : _data.Length - _iterator; }
    }

    public bool HasRemaining(int count)
    {
        return count <= 0 || Remaining >= count;
    }

    public byte ReadB()
    {
        if (_data == null || _iterator >= _data.Length)
            return 0;
        return _data[_iterator++];
    }

    public byte[] ReadB(int length)
    {
        byte[] data = new byte[length];
        Array.Copy(_data, _iterator, data, 0, length);
        _iterator += length;
        return data;
    }

    public int ReadH()
    {
        byte[] data = new byte[2];
        Array.Copy(_data, _iterator, data, 0, 2);
        double value = BitConverter.ToInt16(data, 0);
        _iterator += 2;
        return (int)value;
    }

    public int ReadI()
    {
        if (_iterator + 4 > _data.Length)
            return 0;

        byte[] data = new byte[4];
        Array.Copy(_data, _iterator, data, 0, 4);
        Array.Reverse(data);
        int value = ByteUtils.fromByteArray(data);
        _iterator += 4;
        return value;
    }

    public int ReadSh()
    {
        if (_data == null || _iterator + 2 > _data.Length)
        {
            Debug.LogError("Not enough bytes available to read short value");
            return 0;
        }

        byte[] data = new byte[2];
        Array.Copy(_data, _iterator, data, 0, 2);
        Array.Reverse(data);
        short value = ByteUtils.fromByteArrayShort(data);
        _iterator += 2;
        return value;
    }

    public long ReadL()
    {
        byte[] data = new byte[8];
        Array.Copy(_data, _iterator, data, 0, 8);
        Array.Reverse(data);
        long value = BitConverter.ToInt64(data, 0);
        _iterator += 8;
        return value;
    }

    public long ReadLOther()
    {
        byte[] data = new byte[8];
        Array.Copy(_data, _iterator, data, 0, 8);
        long value = BitConverter.ToInt64(data, 0);
        _iterator += 8;
        return value;
    }

    public double ReadD()
    {
        byte[] data = new byte[8];
        Array.Copy(_data, _iterator, data, 0, 8);
        var value = BitConverter.ToDouble(data, 0);
        _iterator += 8;
        return value;
    }

    public float ReadF()
    {
        byte[] data = new byte[4];
        Array.Copy(_data, _iterator, data, 0, 4);
        Array.Reverse(data);
        float value = BitConverter.ToSingle(data, 0);
        _iterator += 4;
        return value;
    }

    public string ReadS()
    {
        byte strLen = ReadB();
        byte[] data = new byte[strLen];
        Array.Copy(_data, _iterator, data, 0, strLen);
        _iterator += strLen;
        return Encoding.GetEncoding("UTF-8").GetString(data);
    }

    public string ReadOtherS()
    {
        int strLen = 2;
        string text = "";
        try
        {
            for (int i = _iterator; i < _data.Length; i++)
            {
                byte[] data = new byte[strLen];
                Array.Copy(_data, _iterator, data, 0, strLen);
                Array.Reverse(data);
                char str = (char)ByteUtils.fromByteArrayShort(data);
                _iterator += strLen;
                if (str == 0)
                    break;
                text += str;
            }
        }
        catch (Exception)
        {
            Debug.Log("Serverpacket пришла пустая строка !!!");
            return "";
        }

        return text;
    }
}
