using UnityEngine;

[IncomingExPacket(GameExServerPacketType.ExShowSeedInfo)]
public sealed class ExShowSeedInfoIncoming : IncomingWirePacket<ExShowSeedInfoDto>
{
    public override void Apply(ExShowSeedInfoDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            IncomingPacketActions.SeedInfo.ShowWindowActiveTabSeed();
            IncomingPacketActions.SeedInfo.SetDataSeedInfo(packet.List);
        });
    }
}
