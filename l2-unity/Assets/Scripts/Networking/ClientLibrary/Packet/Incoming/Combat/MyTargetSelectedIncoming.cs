using UnityEngine;

[IncomingGamePacket(GameServerPacketType.MyTargetSelected)]
public sealed class MyTargetSelectedIncoming : IncomingWirePacket<MyTargetSelectDto>
{
    public override void Apply(MyTargetSelectDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            if (packet.ObjectId == PlayerEntity.Instance.Identity.Id)
            {
                IncomingPacketActions.Targets.SetTarget(new ObjectData(PlayerEntity.Instance.transform.gameObject), "#ffffff");
            }
            else
            {
                IncomingPacketActions.Targets.NextTargetById(packet.ObjectId, packet.Color);
                IncomingPacketActions.ApplyWorld(apply => apply.SendArrivedPosition());
            }
        });
    }
}
