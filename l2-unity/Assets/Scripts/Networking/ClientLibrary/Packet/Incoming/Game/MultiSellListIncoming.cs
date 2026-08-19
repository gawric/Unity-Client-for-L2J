using UnityEngine;

[IncomingGamePacket(GameServerPacketType.MultiSellList)]
public sealed class MultiSellListIncoming : IncomingWirePacket<MultiSellListDto>
{
    public override void Apply(MultiSellListDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            IncomingPacketActions.MultiSell.AddData(packet.GetOnlyItems(), packet);
            IncomingPacketActions.MultiSell.ShowWindow();
        });
    }
}
