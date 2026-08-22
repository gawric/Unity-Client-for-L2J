[IncomingGamePacket(GameServerPacketType.SocialAction)]
public sealed class SocialActionIncoming : IncomingWirePacket<SocialActionDto>
{
    public override void Apply(SocialActionDto packet)
    {
        IncomingPacketActions.ApplyWorld(apply => apply.SocialAction(packet));
    }
}
