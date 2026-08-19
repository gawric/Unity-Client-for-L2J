[IncomingGamePacket(GameServerPacketType.TradePressOtherOk)]
public sealed class TradePressOtherOkIncoming : IncomingPacket<TradePressOtherOkIncomingDto>
{
    public override TradePressOtherOkIncomingDto Read(PacketReader reader)
    {
        return new TradePressOtherOkIncomingDto();
    }

    public override void Apply(TradePressOtherOkIncomingDto dto)
    {
    }
}
