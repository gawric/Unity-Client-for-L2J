[IncomingGamePacket(GameServerPacketType.TradeDone)]
public sealed class TradeDoneIncoming : IncomingPacket<TradeDoneIncomingDto>
{
    public override TradeDoneIncomingDto Read(PacketReader reader)
    {
        return new TradeDoneIncomingDto();
    }

    public override void Apply(TradeDoneIncomingDto dto)
    {
        IncomingPacketActions.Trade.HideWindow();
    }
}
