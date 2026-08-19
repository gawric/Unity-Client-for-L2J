using UnityEngine;

[IncomingGamePacket(GameServerPacketType.TutorialShowHtml)]
public sealed class TutorialShowHtmlIncoming : IncomingWirePacket<TutorialShowHtmlDto>
{
    public override void Apply(TutorialShowHtmlDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            IncomingPacketActions.Html.InjectToWindow(packet.Html);
            IncomingPacketActions.Html.ShowWindowToCenter();
        });
    }
}
