using UnityEngine;

[IncomingGamePacket(GameServerPacketType.ShortBuffStatusUpdate)]
public sealed class ShortBuffStatusUpdateIncoming : IncomingWirePacket<ShortBuffStatusUpdateDto>
{
    public override void Apply(ShortBuffStatusUpdateDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            if (IncomingPacketActions.Buffer == null || packet.Effect == null)
                return;
            var item = packet.Effect;
            IncomingPacketActions.Buffer.AddDataCellToTime(item.Id, item.Value, item.Duration);
        });
    }
}
