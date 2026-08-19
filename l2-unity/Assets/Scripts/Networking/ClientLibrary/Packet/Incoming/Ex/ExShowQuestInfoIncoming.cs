using UnityEngine;

[IncomingExPacket(GameExServerPacketType.ExShowQuestInfo)]
public sealed class ExShowQuestInfoIncoming : IncomingWirePacket<ExShowQuestInfoDto>
{
    public override void Apply(ExShowQuestInfoDto packet)
    {
        if (IncomingPacketActions.GameWorld != null)
            IncomingPacketActions.Queue(() => IncomingPacketActions.QuestList.ShowWindow());

        Debug.Log("Event Open ExShowQuestInfo Info");
    }
}
