using UnityEngine;

[IncomingGamePacket(GameServerPacketType.EnchantResult)]
public sealed class EnchantResultIncoming : IncomingWirePacket<EnchantResultDto>
{
    public override void Apply(EnchantResultDto packet)
    {
        IncomingPacketActions.Queue(() => IncomingPacketActions.Enchant.EnchantResult(packet.Result));
    }
}
