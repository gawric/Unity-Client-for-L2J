using System.Collections.Generic;

[OutgoingCommandPacket(typeof(RequestBuyItemCommand))]
public sealed class RequestBuyItem : OutgoingWirePacket<RequestBuyItemDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestBuyItem;
    protected override OutgoingBuildKind BuildKind => OutgoingBuildKind.GameNoPad;

    public RequestBuyItem(RequestBuyItemCommand command) : this(command.ListId, command.BuyList) { }

    public RequestBuyItem(int listId, List<Product> buyList)
    {
        Dto.ListId = listId;
        Dto.Items = buyList;
    }
}
