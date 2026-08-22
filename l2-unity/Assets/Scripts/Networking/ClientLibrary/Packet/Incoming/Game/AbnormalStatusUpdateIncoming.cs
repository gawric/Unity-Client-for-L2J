using UnityEngine;

[IncomingGamePacket(GameServerPacketType.AbnormalStatusUpdate)]
public sealed class AbnormalStatusUpdateIncoming : IncomingWirePacket<AbnormalStatusUpdateDto>
{
    public override void Apply(AbnormalStatusUpdateDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            if (IncomingPacketActions.Buffer == null)
                return;
            foreach (var item in packet.ListEffect)
                IncomingPacketActions.Buffer.AddDataCellToTime(item.Id, item.Value, item.Duration);
        });
    }
}
