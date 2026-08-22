using UnityEngine;

/// <summary>Asks the server to drop Count of inventory item ObjectId at Position.</summary>
[OutgoingCommandPacket(typeof(RequestDropItemCommand))]
public sealed class RequestDropItem : OutgoingWirePacket<RequestDropItemDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestDropItem;

    public RequestDropItem(RequestDropItemCommand command) : this(command.ObjectId, command.Count, command.Position) { }

    public RequestDropItem(int objectId, int count, Vector3 position)
    {
        Dto.ObjectId = objectId;
        Dto.Count = count;
        Dto.Position = position;
    }
}
