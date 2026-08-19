/// Wire DTO: packet fields plus ReadFrom. No world/UI side effects.
/// Treated as INetworkModel until chronicle-specific DTOs map to shared snapshots.
public interface IWireDto : INetworkModel
{
    void ReadFrom(PacketReader reader);
}
