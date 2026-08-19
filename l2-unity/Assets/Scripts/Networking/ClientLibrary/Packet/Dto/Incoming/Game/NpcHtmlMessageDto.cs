public class NpcHtmlMessageDto : IWireDto
{
    private int _npcObjId;
    private string _html;
    private int _itemId;

    public string Html => _html;

    

    public int GetNpcId()
    {
        return _npcObjId;
    }

    public void ReadFrom(PacketReader reader)
    {
        _npcObjId = reader.ReadI();
        _html = reader.ReadOtherS();
        _itemId = reader.ReadI();
    }
}
