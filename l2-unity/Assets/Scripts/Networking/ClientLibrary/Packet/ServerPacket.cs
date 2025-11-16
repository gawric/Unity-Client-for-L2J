using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using UnityEngine.XR;
using static UnityEngine.ProBuilder.AutoUnwrapSettings;
using System.Xml.Linq;

public abstract class ServerPacket : Packet
{
    protected byte minimumLength;
    private int _iterator;

    public ServerPacket(byte[] d) : base(d) {
        ReadB();
    }

    protected byte ReadB() {
        return _packetData[_iterator++];
    }

    protected byte[] ReadB(int length) {
        byte[] data = new byte[length];
        Array.Copy(_packetData, _iterator, data, 0, length);
        _iterator += length;

        return data;
    }

    protected int ReadH()
    {
        byte[] data = new byte[2];
        Array.Copy(_packetData, _iterator, data, 0, 2);
        // Array.Reverse(data);
        double value = BitConverter.ToInt16(data, 0);
        _iterator += 2;
        return (int)value;
    }


    protected int ReadI() {
        //if (_iterator >= _packetData.Length) return 0;
        if (_iterator + 4 > _packetData.Length) return 0;
        byte[] data = new byte[4];
        Array.Copy(_packetData, _iterator, data, 0, 4);
        Array.Reverse(data);
        int value = ByteUtils.fromByteArray(data);

        _iterator += 4;
        return value;
    }

    protected int ReadSh()
    {

        if (_packetData == null || _iterator + 2 > _packetData.Length)
        {
            Debug.LogError("Not enough bytes available to read short value");
            return 0; // или можно выбросить исключение
        }

        byte[] data = new byte[2];
        Array.Copy(_packetData, _iterator, data, 0, 2);
        Array.Reverse(data);
 
        short value = ByteUtils.fromByteArrayShort(data);

        _iterator += 2;
        return value;
    }


    protected long ReadL() {
        byte[] data = new byte[8];
        Array.Copy(_packetData, _iterator, data, 0, 8);
        Array.Reverse(data);
        long value = BitConverter.ToInt64(data, 0);
        _iterator += 8;
        return value;
    }

  
    protected long ReadLOther()
    {
        byte[] data = new byte[8];
        Array.Copy(_packetData, _iterator, data, 0, 8);
        //Array.Reverse(data);
        long value = BitConverter.ToInt64(data, 0);
        _iterator += 8;
        return value;
    }

    protected long ReadOtherL()
    {
        byte[] data = new byte[8];
        Array.Copy(_packetData, _iterator, data, 0, 8);
       // Array.Reverse(data);
        long value = BitConverter.ToInt64(data, 0);
        _iterator += 8;
        return value;
    }

    //l2j writeLong(Double.doubleToRawLongBits(value));
    protected double ReadD()
    {
        byte[] data = new byte[8];
        Array.Copy(_packetData, _iterator, data, 0, 8);
        var value =  BitConverter.ToDouble(data, 0);
        _iterator += 8;
        return value;
    }

    protected float ReadF() {
        byte[] data = new byte[4];     
        Array.Copy(_packetData, _iterator, data, 0, 4);
        Array.Reverse(data);
        float value = BitConverter.ToSingle(data, 0);
        _iterator += 4;
        return value; 
    }

    protected string ReadS() {
        byte strLen = ReadB();
        byte[] data = new byte[strLen];
        Array.Copy(_packetData, _iterator, data, 0, strLen);
        _iterator += strLen;

        return Encoding.GetEncoding("UTF-8").GetString(data);
    }
    //java server 
    protected string ReadOtherS()
    {
        int strLen = 2;
        string text = "";
        try
        {
            for (int i = _iterator; i < _packetData.Length; i++)
            {
                byte[] data = new byte[strLen];
                Array.Copy(_packetData, _iterator, data, 0, strLen);
                Array.Reverse(data);
                 char str = (char)ByteUtils.fromByteArrayShort(data);
                _iterator += strLen;
                if (str == 0) break;
                
                 text += str;
            }

        }
        catch(Exception e)
        {
            Debug.Log("Serverpacket пришла пустая строка !!!");
            return "";
        }

        return text;
    }

    public abstract void Parse();
}
