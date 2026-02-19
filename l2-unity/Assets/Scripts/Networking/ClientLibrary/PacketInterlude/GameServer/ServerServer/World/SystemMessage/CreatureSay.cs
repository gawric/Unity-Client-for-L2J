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

        ChatTypeData data = ChatTypes.GetById(chatType);

        if(data != null)
        {
              senderName = ReadOtherS();

              ReadI();//High Five NPCString ID

              text = ReadOtherS();

              int dataType = data.Type;

              if(dataType == 10 || dataType == 18)
              {
                message = new CreatureMessage("Announcements", text , data);
              }
              else
              {
                   message = new CreatureMessage(senderName , text , data);
              }
        }
    }
}
