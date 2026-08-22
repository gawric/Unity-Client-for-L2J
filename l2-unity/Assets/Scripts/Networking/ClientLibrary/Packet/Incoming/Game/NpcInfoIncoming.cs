using UnityEngine;

[IncomingGamePacket(GameServerPacketType.NpcInfo)]
public sealed class NpcInfoIncoming : IncomingWirePacket<NpcInfoDto>
{
    public override void Apply(NpcInfoDto packet)
    {
        StorageNpc.getInstance().AddNpcInfo(packet);
        IncomingPacketActions.QueueWorld(apply => apply.UpdateNpc(packet));
    }
}
