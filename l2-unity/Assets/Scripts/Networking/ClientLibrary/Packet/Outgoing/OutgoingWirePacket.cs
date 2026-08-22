/// Outgoing packet: fills a DTO, then PacketWriter builds opcode/pad/checksum.
public abstract class OutgoingWirePacket<TDto> : IOutgoingPacket where TDto : class, IOutgoingDto, new()
{
    protected TDto Dto = new TDto();

    protected abstract byte Opcode { get; }

    protected virtual OutgoingBuildKind BuildKind => OutgoingBuildKind.GamePad;

    protected virtual int LoginExtraZeroBytes => 0;

    public byte GetPacketType()
    {
        return Opcode;
    }

    private byte[] _built;

    public virtual byte[] GetData()
    {
        if (_built != null)
            return _built;

        PacketWriter writer = new PacketWriter();
        Dto.WriteTo(writer);
        switch (BuildKind)
        {
            case OutgoingBuildKind.GameNoPad:
                _built = writer.BuildGameNoPad(Opcode);
                break;
            case OutgoingBuildKind.GameOverwriteOpcode:
                _built = writer.BuildGameOverwriteOpcode(Opcode);
                break;
            case OutgoingBuildKind.Login:
                _built = writer.BuildLogin(Opcode, LoginExtraZeroBytes);
                break;
            default:
                _built = writer.BuildGame(Opcode);
                break;
        }

        return _built;
    }
}
