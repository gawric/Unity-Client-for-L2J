[IncomingGamePacket(GameServerPacketType.TradeUpdate)]
public sealed class TradeUpdateIncoming : IncomingPacket<TradeUpdateIncomingDto>
{
    public override TradeUpdateIncomingDto Read(PacketReader reader)
    {
        return new TradeUpdateIncomingDto();
    }

    public override void Apply(TradeUpdateIncomingDto dto)
    {
    }
}
