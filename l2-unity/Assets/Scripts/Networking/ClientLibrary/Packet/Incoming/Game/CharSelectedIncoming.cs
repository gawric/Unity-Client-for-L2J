using UnityEngine;

[IncomingGamePacket(GameServerPacketType.CharSelected)]
public sealed class CharSelectedIncoming : IncomingWirePacket<CharSelectedDto>
{
    protected override void OnParsed(CharSelectedDto packet)
    {
        IncomingPacketActions.Game.PlayerInfo = packet.PlayeInfo;
        IncomingPacketActions.Game.SetDataPreparationCompleted(false);
    }

    public override void Apply(CharSelectedDto packet)
    {
        IncomingPacketActions.Game.PlayerInfo = packet.PlayeInfo;
        IncomingPacketActions.Manager.OnCharacterSelect();
    }
}
