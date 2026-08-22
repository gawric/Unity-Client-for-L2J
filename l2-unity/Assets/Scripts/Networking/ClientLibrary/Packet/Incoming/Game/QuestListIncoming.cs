using UnityEngine;

[IncomingGamePacket(GameServerPacketType.QuestList)]
public sealed class QuestListIncoming : IncomingWirePacket<QuestListDto>
{
    public override void Apply(QuestListDto packet)
    {
        IncomingPacketActions.Queue(() => IncomingPacketActions.Quest.AddData(packet.Quest));
    }
}
