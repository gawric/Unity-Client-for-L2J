using UnityEngine;

[IncomingGamePacket(GameServerPacketType.SendTradeRequest)]
public sealed class SendTradeRequestIncoming : IncomingWirePacket<SendTradeRequestDto>
{
    public override void Apply(SendTradeRequestDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            IncomingPacketActions.TradeRequest.AddData(packet);
            IncomingPacketActions.TradeRequest.ShowWindow();
        });
    }
}
