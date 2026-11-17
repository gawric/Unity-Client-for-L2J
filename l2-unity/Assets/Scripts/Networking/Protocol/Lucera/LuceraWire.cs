using System;
using System.Collections.Generic;

/// <summary>Armado de paquetes Lucera 166 en little-endian.</summary>
public static class LuceraWire
{
    public static byte[] ProtocolVersion(int version)
    {
        List<byte> data = new List<byte>(5) { 0x0E };
        AddInt32(data, version);
        return data.ToArray();
    }

    public static byte[] AuthLogin(AuthLoginCommand command)
    {
        List<byte> data = new List<byte>(64) { 0x2B };
        AddL2String(data, command.Account);
        AddInt32(data, command.PlayKey2);
        AddInt32(data, command.PlayKey1);
        AddInt32(data, command.LoginKey1);
        AddInt32(data, command.LoginKey2);
        AddInt32(data, 0);
        AddInt64(data, 0L);
        AddInt32(data, 0);
        return data.ToArray();
    }

    public static byte[] CharacterSelected(int slot)
    {
        List<byte> data = new List<byte>(5) { 0x12 };
        AddInt32(data, slot);
        return data.ToArray();
    }

    public static byte[] CharacterDelete(int slot)
    {
        List<byte> data = new List<byte>(5) { 0x0D };
        AddInt32(data, slot);
        return data.ToArray();
    }

    /// <summary>
    /// CharacterCreate (0x0C). Orden de campos de
    /// ref/lucera-decompilado/.../c2s/CharacterCreate.java: nombre, raza, sexo, clase,
    /// seis ceros y apariencia (hairStyle, hairColor, face).
    /// </summary>
    public static byte[] CharacterCreate(
        string name,
        int race,
        int sex,
        int classId,
        int baseStr,
        int baseDex,
        int baseCon,
        int baseInt,
        int baseWit,
        int baseMen,
        int hairStyle,
        int hairColor,
        int face)
    {
        List<byte> data = new List<byte>(64) { 0x0C };
        AddL2String(data, name);
        AddInt32(data, race);
        AddInt32(data, sex);
        AddInt32(data, classId);
        AddInt32(data, baseStr);
        AddInt32(data, baseDex);
        AddInt32(data, baseCon);
        AddInt32(data, baseInt);
        AddInt32(data, baseWit);
        AddInt32(data, baseMen);
        AddInt32(data, hairStyle);
        AddInt32(data, hairColor);
        AddInt32(data, face);
        return data.ToArray();
    }

    public static byte[] NoPayload(byte opcode)
    {
        return new[] { opcode };
    }

    public static byte[] Say2(string message, int type, string target)
    {
        List<byte> data = new List<byte>(64) { 0x49 };
        AddL2String(data, message);
        AddInt32(data, type);
        AddL2String(data, target);
        return data.ToArray();
    }

    public static byte[] MoveTo(int x, int y, int z, int fromX, int fromY, int fromZ)
    {
        List<byte> data = new List<byte>(29) { 0x0F };
        AddInt32(data, x);
        AddInt32(data, y);
        AddInt32(data, z);
        AddInt32(data, fromX);
        AddInt32(data, fromY);
        AddInt32(data, fromZ);
        AddInt32(data, 1);
        return data.ToArray();
    }

    public static byte[] UseItem(int objectId, int ctrlPressed)
    {
        List<byte> data = new List<byte>(9) { 0x19 };
        AddInt32(data, objectId);
        AddInt32(data, ctrlPressed);
        return data.ToArray();
    }

    public static byte[] Action(int objectId, int x, int y, int z, byte actionId)
    {
        List<byte> data = new List<byte>(18) { 0x1F };
        AddInt32(data, objectId);
        AddInt32(data, x);
        AddInt32(data, y);
        AddInt32(data, z);
        data.Add(actionId);
        return data.ToArray();
    }

    public static byte[] MagicSkillUse(int skillId, int ctrlPressed, byte shiftPressed)
    {
        List<byte> data = new List<byte>(10) { 0x39 };
        AddInt32(data, skillId);
        AddInt32(data, ctrlPressed);
        data.Add(shiftPressed);
        return data.ToArray();
    }

    public static byte[] Bypass(string bypass)
    {
        List<byte> data = new List<byte>(64) { 0x23 };
        AddL2String(data, bypass);
        return data.ToArray();
    }

    public static byte[] TargetCancel(ushort unselect)
    {
        List<byte> data = new List<byte>(3) { 0x48 };
        AddUInt16(data, unselect);
        return data.ToArray();
    }

    public static byte[] RestartPoint(int type)
    {
        List<byte> data = new List<byte>(5) { 0x7D };
        AddInt32(data, type);
        return data.ToArray();
    }

    public static byte[] SkillCoolTime()
    {
        return new byte[] { 0xA6 };
    }

    public static byte[] ShowBoard(int id)
    {
        List<byte> data = new List<byte>(5) { 0x5E };
        AddInt32(data, id);
        return data.ToArray();
    }

    public static byte[] AnswerTrade(int response)
    {
        List<byte> data = new List<byte>(5) { 0x55 };
        AddInt32(data, response);
        return data.ToArray();
    }

    public static byte[] AddTradeItem(int trade, int objectId, int count)
    {
        List<byte> data = new List<byte>(17) { 0x1B };
        AddInt32(data, trade);
        AddInt32(data, objectId);
        AddInt64(data, count);
        return data.ToArray();
    }

    public static byte[] AnswerJoinParty(int response)
    {
        List<byte> data = new List<byte>(5) { 0x43 };
        AddInt32(data, response);
        return data.ToArray();
    }

    public static byte[] MultiSellChoose(int listId, int entryId, int amount)
    {
        List<byte> data = new List<byte>(17) { 0xB0 };
        AddInt32(data, listId);
        AddInt32(data, entryId);
        AddInt64(data, amount);
        return data.ToArray();
    }

    public static byte[] DestroyItem(int objectId, int count)
    {
        List<byte> data = new List<byte>(13) { 0x60 };
        AddInt32(data, objectId);
        AddInt64(data, count);
        return data.ToArray();
    }

    /// <summary>RequestDropItem (0x17): D objectId, Q count, D x, D y, D z.</summary>
    public static byte[] DropItem(int objectId, long count, int x, int y, int z)
    {
        List<byte> data = new List<byte>(25) { 0x17 };
        AddInt32(data, objectId);
        AddInt64(data, count);
        AddInt32(data, x);
        AddInt32(data, y);
        AddInt32(data, z);
        return data.ToArray();
    }

    public static byte[] Ints(byte opcode, params int[] values)
    {
        List<byte> data = new List<byte>(1 + (values != null ? values.Length * 4 : 0)) { opcode };
        if (values != null)
        {
            for (int i = 0; i < values.Length; i++)
            {
                AddInt32(data, values[i]);
            }
        }

        return data.ToArray();
    }

    public static byte[] StringInts(byte opcode, string value, params int[] values)
    {
        List<byte> data = new List<byte>(64) { opcode };
        AddL2String(data, value);
        if (values != null)
        {
            for (int i = 0; i < values.Length; i++)
            {
                AddInt32(data, values[i]);
            }
        }

        return data.ToArray();
    }

    public static byte[] NickName(string memberName, string title)
    {
        List<byte> data = new List<byte>(64) { 0x0B };
        AddL2String(data, memberName);
        AddL2String(data, title);
        return data.ToArray();
    }

    public static byte[] EnchantItem(int objectId)
    {
        List<byte> data = new List<byte>(9) { 0x5F };
        AddInt32(data, objectId);
        AddInt32(data, 0);
        return data.ToArray();
    }

    private static void AddL2String(List<byte> data, string value)
    {
        string text = value ?? string.Empty;
        if (text.Length > 32)
        {
            text = text.Substring(0, 32);
        }

        // readS() en Lucera lee UTF-16LE terminado en ushort 0 (no length-prefix).
        for (int i = 0; i < text.Length; i++)
        {
            AddUInt16(data, text[i]);
        }

        AddUInt16(data, 0);
    }

    private static void AddUInt16(List<byte> data, ushort value)
    {
        data.Add((byte)value);
        data.Add((byte)(value >> 8));
    }

    private static void AddInt32(List<byte> data, int value)
    {
        data.AddRange(BitConverter.GetBytes(value));
    }

    private static void AddInt64(List<byte> data, long value)
    {
        data.AddRange(BitConverter.GetBytes(value));
    }
}

/// <summary>Paquete Lucera ya serializado, listo para la cola de envío.</summary>
public sealed class LuceraRawOutgoing : IOutgoingPacket
{
    private readonly byte[] _data;

    public LuceraRawOutgoing(byte[] data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        if (_data.Length == 0)
        {
            throw new ArgumentException("El paquete no puede estar vacío.", nameof(data));
        }
    }

    public byte GetPacketType()
    {
        return _data[0];
    }

    public byte[] GetData()
    {
        return _data;
    }
}
