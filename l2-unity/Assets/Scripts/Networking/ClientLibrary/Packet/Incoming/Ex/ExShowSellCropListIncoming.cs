using UnityEngine;

[IncomingExPacket(GameExServerPacketType.ExShowSellCropList)]
public sealed class ExShowSellCropListIncoming : IncomingWirePacket<ExShowSellCropListDto>
{
    public override void Apply(ExShowSellCropListDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            IncomingPacketActions.SellCrop.ShowWindow();
            IncomingPacketActions.SellCrop.SetDataTable(packet);
        });
    }
}
