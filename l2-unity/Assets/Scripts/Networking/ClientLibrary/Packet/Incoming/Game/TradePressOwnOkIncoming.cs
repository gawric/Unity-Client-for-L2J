[IncomingGamePacket(GameServerPacketType.TradePressOwnOk)]
public sealed class TradePressOwnOkIncoming : IncomingPacket<TradePressOwnOkIncomingDto>
{
    public override TradePressOwnOkIncomingDto Read(PacketReader reader)
    {
        return new TradePressOwnOkIncomingDto();
    }

    public override void Apply(TradePressOwnOkIncomingDto dto)
    {
    }
}
