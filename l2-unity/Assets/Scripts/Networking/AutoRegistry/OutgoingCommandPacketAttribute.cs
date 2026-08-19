using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class OutgoingCommandPacketAttribute : Attribute
{
    public Type CommandType { get; }

    public OutgoingCommandPacketAttribute(Type commandType)
    {
        CommandType = commandType;
    }
}
