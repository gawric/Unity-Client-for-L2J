using FMOD.Studio;
using L2_login;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.Rendering.DebugUI;

public abstract class ClientPacket : Packet {
    private List<byte> _buffer = new List<byte>();

    public ClientPacket(byte type) : base(type) {}
    public ClientPacket(byte[] data) : base(data) {
        BuildPacket();
    }

    public void WriteB(byte b) {
        _buffer.Add(b);
    }

    public void WriteB(byte[] b) {
        foreach (byte b2 in b) {
            _buffer.Add(b2);
        }
    }


    public void WriteS(String s) {
        Write(Encoding.GetEncoding("UTF-8").GetBytes(s)); 
    }

    public void WriteSOther(String s)
    {
        byte[] data = Encoding.GetEncoding("UTF-16").GetBytes(s);
        Write(data);
    }
    public void WriteSOtherUnicode(String s)
    {
        byte[] data = Encoding.Unicode.GetBytes(s);
        Write(data);
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
        byte[] data = ByteUtils.toByteArray(value);
        _buffer.AddRange(data);
    }

    //use acis server
    protected void WriteOtherS(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            foreach (char c in text)
            {
                // little-endian
                _buffer.Add((byte)(c & 0xFF));
                _buffer.Add((byte)(c >> 8));
            }
        }

        // end string.  use write(000)l2j 
        _buffer.Add(0);
        _buffer.Add(0);
    }

    public void WriteI(int i) {
        // byte[] data = BitConverter.GetBytes(i);

         byte[] data = ByteUtils.toByteArray(i);
         Array.Reverse(data);
        _buffer.AddRange(data);
    }

    public void WriteCheckSum()
    {
        NewCrypt.AppendChecksumWord(_buffer, 0, 4, true);
    }


    public void WriteF(float i) {
        byte[] data = BitConverter.GetBytes(i);
        Array.Reverse(data);
        _buffer.AddRange(data);
    }

    private void Write(byte[] data) {
        _buffer.Add((byte)data.Length);
        _buffer.AddRange(data);
    }

    protected void BuildPacket() {
        _buffer.Insert(0, _packetType);

        // Padding for checksum
        WriteI(0);

        PadBuffer();

        byte[] array = _buffer.ToArray();
        SetData(array);
    }

    protected void BuildPacketExperimental(int addZeroBytes)
    {
        try
        {
            _buffer.Insert(0, _packetType);

            //Padding for balance checksum equls 0
            WriteCheckSum();

            // Padding for checksum
            WriteI(0);

            for(int i=0; i < addZeroBytes; i++)
            {
                WriteB((byte)0);
            }

            PadBuffer();

            byte[] array = _buffer.ToArray();
            SetData(array);
        }
        catch (Exception ex)
        {
            Debug.LogError("ClientPacket->BuildPacketExperimental: " + ex.Message);
        }

    }

    //Acis Ignores packet alignment to 8 or 4
    //Necessary for correct sending of packets since there is no alignment, and Acis checks that each item is equal to 8 bytes without extra zeros
    protected void BuildPacketNoPad()
    {
        _buffer.Insert(0, _packetType);
        byte[] array = _buffer.ToArray();
        SetData(array);
    }

    protected void BuildPacketOther()
    {
        //_buffer.Insert(0, _packetType);

        // Padding for checksum
        WriteI(0);

        PadBuffer();

        byte[] array = _buffer.ToArray();
        // Debug.Log("Set Data " + StringUtils.ByteArrayToString(array));
        //Debug.Log("Size packet " + array.Length);
        SetData(array);
    }

    protected void BuildPacketManualType(byte manualType)
    {
        _buffer[0] =  manualType;

        // Padding for checksum
        WriteI(0);

        PadBuffer();

        byte[] array = _buffer.ToArray();
        // Debug.Log("Set Data " + StringUtils.ByteArrayToString(array));
        //Debug.Log("Size packet " + array.Length);
        SetData(array);
    }
    private void PadBuffer() {
        byte paddingLength = (byte)(_buffer.Count % 8);

       // Debug.Log("Count");
        //Debug.Log(_buffer.Count);
        //Debug.Log(paddingLength);

        if (paddingLength > 0) {

            paddingLength = (byte)(8 - paddingLength);
            //Debug.Log("For each");
            //Debug.Log(paddingLength);
            //Debug.Log($"Packet needs a padding of {paddingLength} bytes.");

            for (int i = 0; i < paddingLength; i++) {
                _buffer.Add((byte)0);
            }
        }

    }
}