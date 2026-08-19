[OutgoingCommandPacket(typeof(RequestQuestAbortCommand))]
public sealed class RequestQuestAbort : OutgoingWirePacket<RequestQuestAbortDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestQuestAbort;

    public RequestQuestAbort(RequestQuestAbortCommand command) : this(command.QuestId) { }

    public RequestQuestAbort(int questId)
    {
        Dto.QuestId = questId;
    }
}
