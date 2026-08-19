using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// Finds outgoing packet classes by command attribute and builds command → packet factories.
public sealed class OutgoingPacketAutoRegistry : PacketAutoRegistry, IOutgoingPacketAutoRegistry
{
    private readonly Dictionary<Type, Func<INetworkCommand, IOutgoingPacket>> _map =
        new Dictionary<Type, Func<INetworkCommand, IOutgoingPacket>>();

    protected override Type Contract
    {
        get { return typeof(IOutgoingPacket); }
    }

    protected override void Register(Type type)
    {
        object[] attrs = type.GetCustomAttributes(typeof(OutgoingCommandPacketAttribute), false);
        if (attrs.Length == 0)
            return;

        OutgoingCommandPacketAttribute attr = (OutgoingCommandPacketAttribute)attrs[0];
        Type commandType = attr.CommandType;
        if (commandType == null)
        {
            Debug.LogError("OutgoingCommandPacket on " + type.Name + " has no command type.");
            return;
        }

        Func<INetworkCommand, IOutgoingPacket> factory = BuildFactory(type, commandType);
        if (factory == null)
        {
            Debug.LogError("OutgoingPacketAutoRegistry: " + type.Name +
                " needs ctor(" + commandType.Name + ") or a parameterless ctor.");
            return;
        }

        TryAddUnique(_map, commandType, factory, type, "outgoing command " + commandType.Name);
    }

    public IOutgoingPacket Create(INetworkCommand command)
    {
        if (command == null)
            return null;

        Func<INetworkCommand, IOutgoingPacket> factory;
        if (!_map.TryGetValue(command.GetType(), out factory))
        {
            Debug.LogError("OutgoingPacketAutoRegistry: no packet for " + command.GetType().Name);
            return null;
        }

        return factory(command);
    }

    private static Func<INetworkCommand, IOutgoingPacket> BuildFactory(Type packetType, Type commandType)
    {
        ConstructorInfo commandCtor = packetType.GetConstructor(new Type[] { commandType });
        if (commandCtor != null)
            return command => (IOutgoingPacket)commandCtor.Invoke(new object[] { command });

        ConstructorInfo emptyCtor = packetType.GetConstructor(Type.EmptyTypes);
        if (emptyCtor != null)
            return command => (IOutgoingPacket)emptyCtor.Invoke(null);

        return null;
    }
}
