using UnityEngine;

[IncomingGamePacket(GameServerPacketType.ShowBoard)]
public sealed class ShowBoardIncoming : IncomingWirePacket<ShowCBoardDto>
{
    public override void Apply(ShowCBoardDto dto)
    {
        if (IncomingPacketActions.GameWorld == null || !dto.ReadyToOpen)
            return;

        IncomingPacketActions.Html.InjectToCommunityWindow(dto.Html);
        IncomingPacketActions.Html.ToggleCommunityBoard(false);
    }
}
