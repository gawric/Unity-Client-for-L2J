using UnityEngine;

[IncomingGamePacket(GameServerPacketType.ItemList)]
public sealed class ItemListIncoming : IncomingWirePacket<ItemListDto>
{
    public override void Apply(ItemListDto packet)
    {
        IncomingPacketActions.Inventory.SetInventory(packet.Items, packet.EquipItems, packet.ShowWindow, packet.AdenaCount, packet.Items.Count + packet.EquipItems.Count);
    }
}
