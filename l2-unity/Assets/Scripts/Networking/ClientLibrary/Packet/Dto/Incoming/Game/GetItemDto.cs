
public class GetItemDto : IWireDto
{
    private int _playerId;
    public int PlayerId { get => _playerId; }
    private ItemInstance _item;
    public ItemInstance Item { get => _item; }


    

    public void ReadFrom(PacketReader reader)
    {
        _playerId = reader.ReadI();
        var objectId = reader.ReadI();
        var x = reader.ReadI();
        var y = reader.ReadI();
        var z = reader.ReadI();
    }
}
