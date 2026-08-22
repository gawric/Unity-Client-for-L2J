using UnityEngine;

[IncomingGamePacket(GameServerPacketType.ChooseInventoryItem)]
public sealed class ChooseInventoryItemIncoming : IncomingWirePacket<ChooseInventoryItemDto>
{
    public override void Apply(ChooseInventoryItemDto packet)
    {
        IncomingPacketActions.Queue(() => IncomingPacketActions.Enchant.ShowWindow(packet.Item));
    }
}
