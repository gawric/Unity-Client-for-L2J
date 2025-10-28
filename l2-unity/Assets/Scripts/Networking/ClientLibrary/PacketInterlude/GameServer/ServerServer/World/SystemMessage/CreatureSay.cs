public class CreatureSay : ServerPacket
{
    private int objectId = 0;
    private int chatType = 0;
    private string senderName;
    private string text;
    private CreatureMessage message;
    public CreatureMessage Message { get { return message; } }
    public CreatureSay(byte[] d) : base(d)
    {
        
        Parse();
    }

    public override void Parse()
    {
        objectId = ReadI();
        chatType = ReadI();
        if (chatType == (int)ChatType.GENERAL)
        {
            senderName = ReadOtherS();
            text = ReadOtherS();
            message = new CreatureMessage(senderName , text , "#dcd9dc");
        }else if ( chatType == (int)ChatType.ANNOUNCEMENT | chatType == (int)ChatType.CRITICAL_ANNOUNCE)
        {
            senderName = ReadOtherS();
            text = ReadOtherS();
            message = new CreatureMessage("Announcements", text , "#80fbff");
        }
    }
}
