[OutgoingCommandPacket(typeof(RequestRecipeBookOpenCommand))]
public sealed class RequestRecipeBookOpen : OutgoingWirePacket<RequestRecipeBookOpenDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestRecipeBookOpen;

    public RequestRecipeBookOpen(RequestRecipeBookOpenCommand command) : this(command.IsDwarven) { }

    public RequestRecipeBookOpen(int isDwarven)
    {
        Dto.IsDwarven = isDwarven;
    }
}
