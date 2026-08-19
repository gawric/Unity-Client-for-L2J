using UnityEngine;

[IncomingExPacket(GameExServerPacketType.ExShowCropInfo)]
public sealed class ExShowCropInfoIncoming : IncomingWirePacket<ExShowCropInfoDto>
{
    public override void Apply(ExShowCropInfoDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            IncomingPacketActions.SeedInfo.SetDataCropInfo(packet.List);
            IncomingPacketActions.SeedInfo.ShowWindow();
        });
    }
}
