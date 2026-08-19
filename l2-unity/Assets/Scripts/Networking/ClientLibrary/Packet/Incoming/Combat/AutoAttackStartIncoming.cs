[IncomingGamePacket(GameServerPacketType.AutoAttackStart)]
public sealed class AutoAttackStartIncoming : IncomingWirePacket<AutoAttackStartDto>
{
    public override void Apply(AutoAttackStartDto packet)
    {
        IncomingPacketActions.ApplyWorld(apply => apply.AutoAttackStart(packet));
    }
}
