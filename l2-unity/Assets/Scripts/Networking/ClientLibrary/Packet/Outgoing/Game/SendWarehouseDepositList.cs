using System.Collections.Generic;

[OutgoingCommandPacket(typeof(SendWarehouseDepositListCommand))]
public sealed class SendWarehouseDepositList : OutgoingWirePacket<SendWarehouseDepositListDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.SendWareHouseDepositList;
    protected override OutgoingBuildKind BuildKind => OutgoingBuildKind.GameNoPad;

    public SendWarehouseDepositList(SendWarehouseDepositListCommand command) : this(command.SellList) { }

    public SendWarehouseDepositList(List<Product> sellList)
    {
        Dto.Items = sellList;
    }
}
