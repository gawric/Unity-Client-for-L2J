[OutgoingCommandPacket(typeof(RequestSkillCoolTimeCommand))]
public sealed class RequestSkillCoolTime : OutgoingWirePacket<RequestSkillCoolTimeDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestSkillCoolTime;
}
