using System;
using UnityEngine;

[OutgoingCommandPacket(typeof(MoveToCommand))]
public sealed class MoveBackwardToLocation : OutgoingWirePacket<MoveBackwardToLocationDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.MoveToLocation;

    public MoveBackwardToLocation(MoveToCommand command) : this(command.From, command.To) { }

    public MoveBackwardToLocation(Vector3 position, Vector3 target)
    {
        Vector3 l2jpos = VectorUtils.ConvertPosUnityToL2j(position);
        Vector3 l2jtar = VectorUtils.ConvertPosUnityToL2j(target);
        Dto.OriginX = (int)Math.Round(l2jpos.x);
        Dto.OriginY = (int)Math.Round(l2jpos.y);
        Dto.OriginZ = (int)Math.Round(l2jpos.z);
        Dto.TargetX = (int)Math.Round(l2jtar.x);
        Dto.TargetY = (int)Math.Round(l2jtar.y);
        Dto.TargetZ = (int)Math.Round(l2jtar.z);
        Dto.CursorMode = 1;
    }
}
