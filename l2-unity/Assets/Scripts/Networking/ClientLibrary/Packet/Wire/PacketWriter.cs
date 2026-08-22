using L2_login;
using System;
using System.Collections.Generic;
using System.Text;

public sealed class PacketWriter
{
    private readonly List<byte> _buffer = new List<byte>();

    public void WriteB(byte b)
    {
        _buffer.Add(b);
    }

    public void WriteB(byte[] b)
    {
        _buffer.AddRange(b);
    }

    public void WriteSOther(string s)
    {
        byte[] data = Encoding.GetEncoding("UTF-16").GetBytes(s ?? string.Empty);
        _buffer.Add((byte)data.Length);
        _buffer.AddRange(data);
    }

    public void WriteOtherS(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            foreach (char c in text)
            {
                _buffer.Add((byte)(c & 0xFF));
                _buffer.Add((byte)(c >> 8));
            }
        }

        _buffer.Add(0);
        _buffer.Add(0);
    }

    public void WriteChar(char value)
    {
        short sho = (short)value;
        byte[] data = ByteUtils.toByteArray(sho);
        Array.Reverse(data);
        _buffer.AddRange(data);
    }

    public void WriteShort(short value)
    {
        byte[] data = ByteUtils.toByteArray(value);
        Array.Reverse(data);
        _buffer.AddRange(data);
    }

    public void WriteOtherShort(short value)
    {
        _buffer.AddRange(ByteUtils.toByteArray(value));
    }

    public void WriteI(int i)
    {
        byte[] data = ByteUtils.toByteArray(i);
        Array.Reverse(data);
        _buffer.AddRange(data);
    }

    public void WriteF(float i)
    {
        byte[] data = BitConverter.GetBytes(i);
        Array.Reverse(data);
        _buffer.AddRange(data);
    }

    public void WriteChecksum()
    {
        NewCrypt.AppendChecksumWord(_buffer, 0, 4, true);
    }

    public void InsertOpcode(byte opcode)
    {
        _buffer.Insert(0, opcode);
    }

    public void WriteZeroInt()
    {
        WriteI(0);
    }

    public void Pad8()
    {
        int paddingLength = _buffer.Count % 8;
        if (paddingLength == 0)
            return;

        paddingLength = 8 - paddingLength;
        for (int i = 0; i < paddingLength; i++)
            _buffer.Add(0);
    }

    public byte[] ToArray()
    {
        return _buffer.ToArray();
    }

    public byte[] BuildGame(byte opcode)
    {
        InsertOpcode(opcode);
        WriteZeroInt();
        Pad8();
        return ToArray();
    }

    public byte[] BuildGameNoPad(byte opcode)
    {
        InsertOpcode(opcode);
        return ToArray();
    }

    public byte[] BuildGameOverwriteOpcode(byte opcode)
    {
        if (_buffer.Count == 0)
            WriteB(opcode);
        else
            _buffer[0] = opcode;
        WriteZeroInt();
        Pad8();
        return ToArray();
    }

    public byte[] BuildLogin(byte opcode, int extraZeroBytes)
    {
        InsertOpcode(opcode);
        WriteChecksum();
        WriteZeroInt();
        for (int i = 0; i < extraZeroBytes; i++)
            WriteB(0);
        Pad8();
        return ToArray();
    }
}
