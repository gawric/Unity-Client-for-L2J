using UnityEngine;

[IncomingExPacket(GameExServerPacketType.ExShowManorDefaultInfo)]
public sealed class ExShowManorDefaultInfoIncoming : IncomingWirePacket<ExShowManorDefaultInfoDto>
{
    public override void Apply(ExShowManorDefaultInfoDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            IncomingPacketActions.SeedInfo.SetDataDefaultManorInfo(packet.List);
            IncomingPacketActions.SeedInfo.ShowWindowActiveTabAllDefault();
        });
    }
}
