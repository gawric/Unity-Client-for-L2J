using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IncomingLoginPacketAttribute : Attribute
{
    public LoginServerPacketType Opcode { get; }

    public IncomingLoginPacketAttribute(LoginServerPacketType opcode)
    {
        Opcode = opcode;
    }
}
