using UnityEngine;

[IncomingGamePacket(GameServerPacketType.MacroList)]
public sealed class MacroListIncoming : IncomingWirePacket<MacroListDto>
{
    public override void Apply(MacroListDto packet)
    {
    }
}
