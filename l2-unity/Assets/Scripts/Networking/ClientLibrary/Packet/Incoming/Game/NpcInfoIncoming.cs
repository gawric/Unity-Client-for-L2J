using UnityEngine;

[IncomingGamePacket(GameServerPacketType.NpcInfo)]
public sealed class NpcInfoIncoming : IncomingWirePacket<NpcInfoDto>
{
    public override void Apply(NpcInfoDto packet)
    {
        if (packet.Identity != null)
            SpawnFrameBudget.ClearCancel(packet.Identity.Id);
        StorageNpc.getInstance().AddNpcInfo(packet);
        IncomingPacketActions.QueueWorld(apply => apply.UpdateNpc(packet));
    }
}
