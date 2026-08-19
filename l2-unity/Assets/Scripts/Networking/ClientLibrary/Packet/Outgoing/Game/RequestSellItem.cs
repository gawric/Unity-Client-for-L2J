using System.Collections.Generic;

[OutgoingCommandPacket(typeof(RequestSellItemCommand))]
public sealed class RequestSellItem : OutgoingWirePacket<RequestSellItemDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestSellItem;
    protected override OutgoingBuildKind BuildKind => OutgoingBuildKind.GameNoPad;

    public RequestSellItem(RequestSellItemCommand command) : this(command.ListId, command.SellList) { }

    public RequestSellItem(int listId, List<Product> sellList)
    {
        Dto.ListId = listId;
        Dto.Items = sellList;
    }
}
