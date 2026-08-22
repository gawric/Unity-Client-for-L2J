[IncomingGamePacket(GameServerPacketType.TradeOtherAdd)]
public sealed class TradeOtherAddIncoming : IncomingPacket<TradeOtherAddIncomingDto>
{
    public override TradeOtherAddIncomingDto Read(PacketReader reader)
    {
        return new TradeOtherAddIncomingDto();
    }

    public override void Apply(TradeOtherAddIncomingDto dto)
    {
    }
}
