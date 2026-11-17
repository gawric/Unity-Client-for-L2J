/// One implementation per chronicle. Commands in, bytes out. Raw bytes in, models out.
public interface IProtocol
{
    /// <summary>Versión que se manda en ProtocolVersion (746 Interlude, 166 Lucera).</summary>
    int ProtocolVersion { get; }

    IOutgoingPacket EncodeGame(INetworkCommand command, bool encrypt);

    IOutgoingPacket EncodeLogin(INetworkCommand command);

    bool TryParseGame(byte[] raw, bool cryptEnabled, out INetworkModel model);

    bool TryParseLogin(byte[] raw, bool init, bool cryptEnabled, out INetworkModel model);

    void SetGameCryptKey(byte[] key);

    void SetLoginBlowfishKey(byte[] key);
}
