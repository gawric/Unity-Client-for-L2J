using System;
using UnityEngine;

/// <summary>
/// Asks the server to drop <paramref name="count"/> of the given inventory item at
/// <paramref name="position"/> - opcode 0x12, body (objectId, count, x, y, z) matches the standard
/// L2J Interlude client protocol.
/// </summary>
public class RequestDropItem : ClientPacket
{
    public RequestDropItem(int objectId, int count, Vector3 position) : base((byte)GameInterludeClientPacketType.RequestDropItem)
    {
        Vector3 l2jPos = VectorUtils.ConvertPosUnityToL2j(position);

        WriteI(objectId);
        WriteI(count);
        WriteI((int)Math.Round(l2jPos.x));
        WriteI((int)Math.Round(l2jPos.y));
        WriteI((int)Math.Round(l2jPos.z));

        BuildPacket();
    }
}
