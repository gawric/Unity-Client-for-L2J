using System;
using UnityEngine;

[IncomingGamePacket(GameServerPacketType.CharSelectionInfo)]
public sealed class CharSelectionInfoIncoming : IncomingWirePacket<CharSelectionInfoDto>
{
    public override void Apply(CharSelectionInfoDto packet)
    {
        int count = packet != null && packet.Characters != null ? packet.Characters.Count : -1;
        LobbyFlowLog.Info("CharSelectionInfo Apply chars=" + count + " selectedSlot=" + (packet != null ? packet.SelectedSlotId : -1));
        MapClassId.Init();
        IncomingPacketActions.Queue(() =>
        {
            try
            {
                LobbyFlowLog.Info("CharSelectionInfo UI SetInterludeCharacterList");
                IncomingPacketActions.CharSelect.SetInterludeCharacterList(packet.Characters);
                IncomingPacketActions.CharSelect.SelectInterludeSlot(packet.SelectedSlotId);

                LobbyFlowLog.Info("CharSelectionInfo spawn lobby pawns");
                IncomingPacketActions.Characters.SetCharacterInterludeList(packet.Characters);
                IncomingPacketActions.Characters.SelectInterludeCharacter(packet.SelectedSlotId);

                IncomingPacketActions.Login.Disconnect();
                LobbyFlowLog.Info("CharSelectionInfo → Game.OnAuthAllowed (show char select)");
                IncomingPacketActions.Game.OnAuthAllowed();
                LobbyFlowLog.Info("CharSelectionInfo DONE state=" +
                    (IncomingPacketActions.Manager != null ? IncomingPacketActions.Manager.GameState.ToString() : "null"));
            }
            catch (Exception ex)
            {
                LobbyFlowLog.Exception("CharSelectionInfo Apply (this packet breaks lobby)", ex);
            }
        });
    }
}
