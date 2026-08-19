using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IncomingGamePacketAttribute : Attribute
{
    public GameServerPacketType Opcode { get; }

    public IncomingGamePacketAttribute(GameServerPacketType opcode)
    {
        Opcode = opcode;
    }
}
