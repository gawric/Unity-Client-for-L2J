[OutgoingCommandPacket(typeof(AttackRequestCommand))]
public sealed class AttackRequest : OutgoingWirePacket<AttackRequestDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.AttackRequest;
    public AttackRequest(AttackRequestCommand command)
    {
        Dto.ObjectId = command.ObjectId;
        Dto.X = command.X;
        Dto.Y = command.Y;
        Dto.Z = command.Z;
    }
}
