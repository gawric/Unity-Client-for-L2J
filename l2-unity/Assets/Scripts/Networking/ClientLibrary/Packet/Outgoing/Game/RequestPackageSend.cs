using System.Collections.Generic;

[OutgoingCommandPacket(typeof(RequestPackageSendCommand))]
public sealed class RequestPackageSend : OutgoingWirePacket<RequestPackageSendDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestPackageSend;
    protected override OutgoingBuildKind BuildKind => OutgoingBuildKind.GameNoPad;

    public RequestPackageSend(RequestPackageSendCommand command) : this(command.ObjectId, command.BuyList) { }

    public RequestPackageSend(int objectId, List<Product> buyList)
    {
        Dto.ObjectId = objectId;
        Dto.Items = buyList;
    }
}
