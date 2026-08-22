using UnityEngine;

public class SocialActionDto : IWireDto
{
    private int _objectId;
    private int _actionId;

    public int ObjectId { get => _objectId; }
    public int ActionId { get => _actionId; }
    

    public void ReadFrom(PacketReader reader)
    {
        _objectId = reader.ReadI();
        _actionId = reader.ReadI();
    }
}
