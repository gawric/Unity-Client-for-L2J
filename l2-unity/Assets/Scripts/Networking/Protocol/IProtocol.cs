/// One implementation per chronicle. Commands in, bytes out. Raw bytes in, models out.
public interface IProtocol
{
    IOutgoingPacket EncodeGame(INetworkCommand command, bool encrypt);

    IOutgoingPacket EncodeLogin(INetworkCommand command);

    bool TryParseGame(byte[] raw, bool cryptEnabled, out INetworkModel model);

    bool TryParseLogin(byte[] raw, bool init, bool cryptEnabled, out INetworkModel model);

    void SetGameCryptKey(byte[] key);

    void SetLoginBlowfishKey(byte[] key);
}
