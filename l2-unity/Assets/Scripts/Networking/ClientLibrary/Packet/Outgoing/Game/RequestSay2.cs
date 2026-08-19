[OutgoingCommandPacket(typeof(RequestSay2Command))]
public sealed class RequestSay2 : OutgoingWirePacket<RequestSay2Dto>
{
    protected override byte Opcode => (byte)GameClientPacketType.Say2;

    public RequestSay2(RequestSay2Command command) : this(command.Data, command.Message, command.TargetName) { }

    public RequestSay2(ChatTypeData data, string message, string name)
    {
        Dto.Data = data;
        Dto.Message = message;
        Dto.Name = name;
    }
}
