using System.Collections.Generic;

[OutgoingCommandPacket(typeof(SendWarehouseWithdrawListCommand))]
public sealed class SendWarehouseWithdrawList : OutgoingWirePacket<SendWarehouseWithdrawListDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.SendWareHouseWithDrawList;
    protected override OutgoingBuildKind BuildKind => OutgoingBuildKind.GameNoPad;

    public SendWarehouseWithdrawList(SendWarehouseWithdrawListCommand command) : this(command.SellList) { }

    public SendWarehouseWithdrawList(List<Product> sellList)
    {
        Dto.Items = sellList;
    }
}
