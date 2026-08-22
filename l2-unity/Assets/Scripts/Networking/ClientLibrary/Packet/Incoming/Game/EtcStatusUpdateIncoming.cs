using UnityEngine;

[IncomingGamePacket(GameServerPacketType.EtcStatusUpdate)]
public sealed class EtcStatusUpdateIncoming : IncomingWirePacket<EtcStatusUpdateDto>
{
    public override void Apply(EtcStatusUpdateDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            if (IncomingPacketActions.Buffer != null)
                IncomingPacketActions.Buffer.RefreshPenalty(packet);
        });
    }
}
