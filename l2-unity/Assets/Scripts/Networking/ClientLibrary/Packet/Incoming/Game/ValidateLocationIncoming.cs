using UnityEngine;

[IncomingGamePacket(GameServerPacketType.ValidateLocation)]
public sealed class ValidateLocationIncoming : IncomingWirePacket<ValidateLocationDto>
{
    public override void Apply(ValidateLocationDto packet)
    {
        IncomingPacketActions.PositionValidation.AddValidateLocation(packet);
    }
}
