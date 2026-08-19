using UnityEngine;

[IncomingGamePacket(GameServerPacketType.ManagePledgePower)]
public sealed class ManagePledgePowerIncoming : IncomingWirePacket<ManagePledgePowerDto>
{
    public override void Apply(ManagePledgePowerDto packet)
    {
        IncomingPacketActions.Queue(() => IncomingPacketActions.Clan.UpdateDetailedInfo(packet));
    }
}
