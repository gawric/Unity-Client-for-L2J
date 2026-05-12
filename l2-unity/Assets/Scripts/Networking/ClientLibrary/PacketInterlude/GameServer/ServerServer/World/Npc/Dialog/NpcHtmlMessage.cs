public class NpcHtmlMessage : ServerPacket
{
    private int _npcObjId;
    private string _html;
    private int _itemId;

    public string Html => _html;

    public NpcHtmlMessage(byte[] d) : base(d)
    {
        Parse();
    }

    public int GetNpcId()
    {
        return _npcObjId;
    }

    public override void Parse()
    {
        _npcObjId = ReadI();
        _html = ReadOtherS();
        _itemId = ReadI();
    }
}
