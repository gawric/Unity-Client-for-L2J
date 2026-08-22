[IncomingGamePacket(GameServerPacketType.TradeOtherAdd)]
public sealed class TradeOtherAddIncoming : IncomingWirePacket<TradeOtherAddDto>
{
    public override void Apply(TradeOtherAddDto dto)
    {
        IncomingPacketActions.Queue(() => IncomingPacketActions.Trade.OtherAddItem(dto));
    }
}
