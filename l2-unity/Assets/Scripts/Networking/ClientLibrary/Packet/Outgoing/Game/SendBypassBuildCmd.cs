[OutgoingCommandPacket(typeof(SendBypassBuildCmdCommand))]
public sealed class SendBypassBuildCmd : OutgoingWirePacket<SendBypassBuildCmdDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.SendBypassBuildCmd;

    public SendBypassBuildCmd(SendBypassBuildCmdCommand command)
    {
        Dto.Command = command.Command;
    }
}
