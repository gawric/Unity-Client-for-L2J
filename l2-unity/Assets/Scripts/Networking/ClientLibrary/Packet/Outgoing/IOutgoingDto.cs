/// Outgoing wire DTO: fields plus WriteTo. No socket or crypt side effects.
public interface IOutgoingDto
{
    void WriteTo(PacketWriter writer);
}
