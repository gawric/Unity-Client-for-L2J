using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public interface IIncomingPacketAutoRegistry : INetworkHandlers
{
    bool IsGameOpcode(byte opcode);

    bool TryParseGame(ItemServer item, out INetworkModel model);

    bool TryParseLogin(ItemLogin item, out INetworkModel model);
}

public interface IOutgoingPacketAutoRegistry
{
    IOutgoingPacket Create(INetworkCommand command);
}

/// Shared assembly scan for attribute-based packet registries.
public abstract class PacketAutoRegistry
{
    protected PacketAutoRegistry()
    {
        Scan(GetType().Assembly);
    }

    protected abstract Type Contract { get; }

    protected abstract void Register(Type type);

    private void Scan(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types;
        }

        for (int i = 0; i < types.Length; i++)
        {
            Type type = types[i];
            if (type == null || type.IsAbstract || !Contract.IsAssignableFrom(type))
                continue;

            Register(type);
        }
    }

    protected static bool TryAddUnique<TKey, TValue>(
        Dictionary<TKey, TValue> map, TKey key, TValue value, Type owner, string kind)
    {
        if (map.ContainsKey(key))
        {
            Debug.LogError("Duplicate " + kind + " for " + owner.Name);
            return false;
        }

        map[key] = value;
        return true;
    }
}
