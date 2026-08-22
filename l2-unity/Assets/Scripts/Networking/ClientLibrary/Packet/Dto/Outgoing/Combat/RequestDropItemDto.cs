using System;
using UnityEngine;

public sealed class RequestDropItemDto : IOutgoingDto
{
    public int ObjectId;
    public int Count;
    public Vector3 Position;

    public void WriteTo(PacketWriter writer)
    {
        Vector3 l2jPos = VectorUtils.ConvertPosUnityToL2j(Position);

        writer.WriteI(ObjectId);
        writer.WriteI(Count);
        writer.WriteI((int)Math.Round(l2jPos.x));
        writer.WriteI((int)Math.Round(l2jPos.y));
        writer.WriteI((int)Math.Round(l2jPos.z));
    }
}
