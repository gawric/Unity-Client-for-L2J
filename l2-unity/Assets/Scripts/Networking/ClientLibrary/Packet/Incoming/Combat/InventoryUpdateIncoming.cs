using UnityEngine;

[IncomingGamePacket(GameServerPacketType.InventoryUpdate)]
public sealed class InventoryUpdateIncoming : IncomingWirePacket<InventoryUpdateDto>
{
    public override void Apply(InventoryUpdateDto packet)
    {
        IncomingPacketActions.Inventory.UpdateInventory(packet.Items, packet.EquipItems);
    }
}
