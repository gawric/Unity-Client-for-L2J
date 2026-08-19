/// Built outgoing packet ready for the send queue.
public interface IOutgoingPacket
{
    byte GetPacketType();
    byte[] GetData();
}
