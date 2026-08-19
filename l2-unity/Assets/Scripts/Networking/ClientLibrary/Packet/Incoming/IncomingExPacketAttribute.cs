using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IncomingExPacketAttribute : Attribute
{
    public GameExServerPacketType Opcode { get; }

    public IncomingExPacketAttribute(GameExServerPacketType opcode)
    {
        Opcode = opcode;
    }
}
