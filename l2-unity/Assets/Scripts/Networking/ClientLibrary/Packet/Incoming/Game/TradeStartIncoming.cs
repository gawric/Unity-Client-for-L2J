using UnityEngine;

[IncomingGamePacket(GameServerPacketType.TradeStart)]
public sealed class TradeStartIncoming : IncomingWirePacket<TradeStartDto>
{
    public override void Apply(TradeStartDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            IncomingPacketActions.Trade.AddData(packet);
            IncomingPacketActions.Trade.ShowWindow();
        });
    }
}
