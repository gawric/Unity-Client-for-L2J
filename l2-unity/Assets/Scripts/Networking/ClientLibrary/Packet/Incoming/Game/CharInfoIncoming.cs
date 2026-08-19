[IncomingGamePacket(GameServerPacketType.CharInfo)]
public sealed class CharInfoIncoming : IncomingWirePacket<CharInfoDto>
{
    public override void Apply(CharInfoDto packet)
    {
        if (packet == null || packet.Identity == null)
            return;

        PlayerEntity local = PlayerEntity.Instance;
        if (local != null && local.Identity != null && local.Identity.Id == packet.Identity.Id)
        {
            GearFlowLog.Info("CharInfo SKIP local id=" + packet.Identity.Id +
                " nick=" + packet.Identity.Name + " " + GearFlowLog.Paperdoll(packet.Appearance));
            return;
        }

        GearFlowLog.Info("CharInfo RECV id=" + packet.Identity.Id +
            " nick=" + packet.Identity.Name + " " + GearFlowLog.Paperdoll(packet.Appearance));
        StorageNpc.getInstance().AddCharInfo(packet);
        IncomingPacketActions.QueueWorld(apply => apply.UpdateUser(packet));
    }
}
