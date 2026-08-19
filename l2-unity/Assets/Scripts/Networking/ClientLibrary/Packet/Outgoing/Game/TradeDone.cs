[OutgoingCommandPacket(typeof(TradeDoneCommand))]
public sealed class TradeDone : OutgoingWirePacket<TradeDoneDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.TradeDone;

    public TradeDone(TradeDoneCommand command) : this(command.Response) { }

    public TradeDone(int response)
    {
        Dto.Response = response;
    }
}
