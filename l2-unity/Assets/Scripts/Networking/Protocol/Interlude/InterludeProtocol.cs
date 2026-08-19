using L2_login;
using UnityEngine;

public sealed class InterludeProtocol : IProtocol
{
    private readonly IIncomingPacketAutoRegistry _packets;
    private readonly IOutgoingPacketAutoRegistry _outgoing;
    private readonly object _gameCryptLock = new object();
    private readonly object _loginCryptLock = new object();
    private GameCrypt _gameCrypt;
    private BlowfishEngine _loginEncrypt;
    private BlowfishEngine _loginDecrypt;

    public InterludeProtocol(IIncomingPacketAutoRegistry packets, IOutgoingPacketAutoRegistry outgoing)
    {
        _packets = packets;
        _outgoing = outgoing;
    }

    public IOutgoingPacket EncodeGame(INetworkCommand command, bool encrypt)
    {
        IOutgoingPacket packet = ToPacket(command);
        if (packet == null)
            return null;

        if (encrypt)
        {
            byte[] data = packet.GetData();
            lock (_gameCryptLock)
            {
                if (_gameCrypt != null)
                    _gameCrypt.Encrypt(data, 0, data.Length);
            }
        }

        return packet;
    }

    public IOutgoingPacket EncodeLogin(INetworkCommand command)
    {
        IOutgoingPacket packet = ToPacket(command);
        if (packet == null)
            return null;

        byte[] data = packet.GetData();
        byte type = packet.GetPacketType();
        if (type != (byte)LoginClientPacketType.AuthGameGuard
            && type != (byte)LoginClientPacketType.RequestAuthLogin
            && type != (byte)LoginClientPacketType.RequestServerList)
        {
            NewCrypt.appendChecksum(data);
        }

        lock (_loginCryptLock)
        {
            if (_loginEncrypt != null)
                _loginEncrypt.processBigBlock(data, 0, data, 0, data.Length);
        }

        return packet;
    }

    public bool TryParseGame(byte[] raw, bool cryptEnabled, out INetworkModel model)
    {
        model = null;
        if (raw == null || raw.Length == 0)
            return false;

        if (cryptEnabled)
        {
            lock (_gameCryptLock)
            {
                if (_gameCrypt != null)
                    _gameCrypt.Decrypt(raw, 0, raw.Length);
            }
        }

        byte opcode = raw[0];
        if (!_packets.IsGameOpcode(opcode))
        {
            LobbyFlowLog.Warn(
                "unknown game opcode 0x" + opcode.ToString("X2") +
                " name=" + LobbyFlowLog.OpcodeName(opcode) +
                " len=" + raw.Length + " crypt=" + cryptEnabled);
            return false;
        }

        bool parsed = _packets.TryParseGame(new ItemServer(raw), out model);
        if (!parsed)
        {
            LobbyFlowLog.Warn(
                "TryParseGame returned false opcode=0x" + opcode.ToString("X2") +
                " " + LobbyFlowLog.OpcodeName(opcode) + " len=" + raw.Length);
        }
        else
        {
            LobbyFlowLog.Info(
                "parsed opcode=0x" + opcode.ToString("X2") + " " + LobbyFlowLog.OpcodeName(opcode) +
                " dto=" + model.GetType().Name + " len=" + raw.Length);
        }

        return parsed;
    }

    public bool TryParseLogin(byte[] raw, bool init, bool cryptEnabled, out INetworkModel model)
    {
        model = null;
        if (raw == null || raw.Length == 0)
            return false;

        if (cryptEnabled)
        {
            lock (_loginCryptLock)
            {
                if (_loginDecrypt != null)
                    _loginDecrypt.processBigBlock(raw, 0, raw, 0, raw.Length);
            }

            if (init)
            {
                if (!NewCrypt.decXORPass(raw))
                {
                    Debug.LogError("Packet XOR could not be decoded.");
                    return false;
                }
            }
            else if (!NewCrypt.verifyChecksum(raw))
            {
                Debug.LogError("Packet checksum is wrong. Ignoring packet... length " + raw.Length);
                return false;
            }
        }

        return _packets.TryParseLogin(new ItemLogin(raw), out model);
    }

    public void SetGameCryptKey(byte[] key)
    {
        lock (_gameCryptLock)
        {
            _gameCrypt = new GameCrypt();
            _gameCrypt.SetKey(key);
        }
    }

    public void SetLoginBlowfishKey(byte[] key)
    {
        lock (_loginCryptLock)
        {
            _loginDecrypt = new BlowfishEngine();
            _loginDecrypt.init(false, key);
            _loginEncrypt = new BlowfishEngine();
            _loginEncrypt.init(true, key);
        }
    }

    private IOutgoingPacket ToPacket(INetworkCommand command)
    {
        return _outgoing.Create(command);
    }
}
