using System.Diagnostics;

public readonly struct IncomingRawPacket
{
    public readonly byte[] Data;
    public readonly bool Init;
    public readonly bool CryptEnabled;
    public readonly long RecvTick;
    public readonly int QueueAhead;

    public IncomingRawPacket(byte[] data, bool init, bool cryptEnabled)
        : this(data, init, cryptEnabled, Stopwatch.GetTimestamp(), 0)
    {
    }

    public IncomingRawPacket(byte[] data, bool init, bool cryptEnabled, long recvTick, int queueAhead)
    {
        Data = data;
        Init = init;
        CryptEnabled = cryptEnabled;
        RecvTick = recvTick;
        QueueAhead = queueAhead;
    }
}
