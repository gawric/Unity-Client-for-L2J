using UnityEngine;

[OutgoingCommandPacket(typeof(ValidatePositionCommand))]
public sealed class ValidatePosition : OutgoingWirePacket<ValidatePositionDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.ValidatePosition;

    public ValidatePosition(ValidatePositionCommand command) : this(command.X, command.Y, command.Z) { }

    public ValidatePosition(float x, float y, float z)
    {
        var location = VectorUtils.ConvertPosUnityToL2j(new Vector3(x, y, z));
        Dto.X = (int)location.x;
        Dto.Y = (int)location.y;
        Dto.Z = (int)location.z;
    }
}
