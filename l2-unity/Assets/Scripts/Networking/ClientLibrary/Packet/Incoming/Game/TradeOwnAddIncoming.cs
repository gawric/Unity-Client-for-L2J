[IncomingGamePacket(GameServerPacketType.TradeOwnAdd)]
public sealed class TradeOwnAddIncoming : IncomingPacket<TradeOwnAddIncomingDto>
{
    public override TradeOwnAddIncomingDto Read(PacketReader reader)
    {
        return new TradeOwnAddIncomingDto();
    }

    public override void Apply(TradeOwnAddIncomingDto dto)
    {
    }
}
