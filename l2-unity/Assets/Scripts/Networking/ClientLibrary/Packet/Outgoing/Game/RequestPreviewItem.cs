using System.Collections.Generic;

[OutgoingCommandPacket(typeof(RequestPreviewItemCommand))]
public sealed class RequestPreviewItem : OutgoingWirePacket<RequestPreviewItemDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestPreviewItem;

    public RequestPreviewItem(RequestPreviewItemCommand command) : this(command.ListId, command.BuyList) { }

    public RequestPreviewItem(int listId, List<Product> buyList)
    {
        Dto.Unknown = 0;
        Dto.ListId = listId;
        Dto.Items = buyList;
    }
}
