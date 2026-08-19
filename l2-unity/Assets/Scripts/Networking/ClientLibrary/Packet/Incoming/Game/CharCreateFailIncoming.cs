using UnityEngine;

[IncomingGamePacket(GameServerPacketType.CharCreateFail)]
public sealed class CharCreateFailIncoming : IncomingWirePacket<CharCreateFailDto>
{
    public override void Apply(CharCreateFailDto packet)
    {
        string text = packet.Text;
        IncomingPacketActions.Queue(() => IncomingPacketActions.Manager.OnCreateUserFail(text));
    }
}
