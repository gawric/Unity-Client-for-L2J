using System;
using System.Collections.Generic;
using UnityEngine;

/// Finds incoming packet classes by opcode attribute and registers them for Parse/Apply.
public sealed class IncomingPacketAutoRegistry : PacketAutoRegistry, IIncomingPacketAutoRegistry
{
    private readonly Dictionary<byte, IIncomingPacketBinding> _game = new Dictionary<byte, IIncomingPacketBinding>();
    private readonly Dictionary<byte, IIncomingPacketBinding> _login = new Dictionary<byte, IIncomingPacketBinding>();
    private readonly Dictionary<int, IIncomingPacketBinding> _ex = new Dictionary<int, IIncomingPacketBinding>();
    private readonly Dictionary<Type, IIncomingPacketBinding> _byModel = new Dictionary<Type, IIncomingPacketBinding>();

    protected override Type Contract
    {
        get { return typeof(IIncomingPacketBinding); }
    }

    protected override void Register(Type type)
    {
        object[] gameAttrs = type.GetCustomAttributes(typeof(IncomingGamePacketAttribute), false);
        if (gameAttrs.Length > 0)
        {
            IncomingGamePacketAttribute attr = (IncomingGamePacketAttribute)gameAttrs[0];
            TryAddUnique(_game, (byte)attr.Opcode, CreateBinding(type), type,
                "incoming packet opcode 0x" + ((byte)attr.Opcode).ToString("X2"));
            return;
        }

        object[] loginAttrs = type.GetCustomAttributes(typeof(IncomingLoginPacketAttribute), false);
        if (loginAttrs.Length > 0)
        {
            IncomingLoginPacketAttribute attr = (IncomingLoginPacketAttribute)loginAttrs[0];
            TryAddUnique(_login, (byte)attr.Opcode, CreateBinding(type), type,
                "incoming packet opcode 0x" + ((byte)attr.Opcode).ToString("X2"));
            return;
        }

        object[] exAttrs = type.GetCustomAttributes(typeof(IncomingExPacketAttribute), false);
        if (exAttrs.Length > 0)
        {
            IncomingExPacketAttribute attr = (IncomingExPacketAttribute)exAttrs[0];
            TryAddUnique(_ex, (int)attr.Opcode, CreateBinding(type), type,
                "incoming ex packet opcode 0x" + ((int)attr.Opcode).ToString("X2"));
        }
    }

    private IIncomingPacketBinding CreateBinding(Type type)
    {
        IIncomingPacketBinding binding = (IIncomingPacketBinding)Activator.CreateInstance(type);
        Type modelType = binding.ModelType;
        if (modelType == null)
            return binding;

        TryAddUnique(_byModel, modelType, binding, type, "incoming model " + modelType.Name);
        return binding;
    }

    public bool IsGameOpcode(byte opcode)
    {
        if (opcode == (byte)GameServerPacketType.ExTypePacket)
            return true;
        return _game.ContainsKey(opcode);
    }

    public bool TryParseGame(ItemServer item, out INetworkModel model)
    {
        model = null;
        byte opcode = item.ByteType();
        IIncomingPacketBinding binding;
        byte[] data;
        if (opcode == (byte)GameServerPacketType.ExTypePacket)
        {
            if (!_ex.TryGetValue(item.ExPacketType(), out binding))
                return false;
            data = item.DecodeExData();
        }
        else if (!_game.TryGetValue(opcode, out binding))
        {
            return false;
        }
        else
        {
            data = item.DecodeData();
        }

        model = binding.Parse(data);
        return model != null;
    }

    public bool TryParseLogin(ItemLogin item, out INetworkModel model)
    {
        model = null;
        IIncomingPacketBinding binding;
        if (!_login.TryGetValue((byte)item.PaketType(), out binding))
            return false;

        model = binding.Parse(item.DecodeData());
        return model != null;
    }

    public void Handle(INetworkModel model)
    {
        if (model == null)
            return;

        IIncomingPacketBinding binding;
        if (!_byModel.TryGetValue(model.GetType(), out binding))
        {
            Debug.LogWarning("No incoming handler for " + model.GetType().Name);
            return;
        }

        binding.ApplyModel(model);
        LobbyFlowLog.Info("Handle Apply done " + model.GetType().Name);
    }
}
