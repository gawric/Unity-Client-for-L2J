public readonly struct IncomingRawPacket
{
    public readonly byte[] Data;
    public readonly bool Init;
    public readonly bool CryptEnabled;

    public IncomingRawPacket(byte[] data, bool init, bool cryptEnabled)
    {
        Data = data;
        Init = init;
        CryptEnabled = cryptEnabled;
    }
}
