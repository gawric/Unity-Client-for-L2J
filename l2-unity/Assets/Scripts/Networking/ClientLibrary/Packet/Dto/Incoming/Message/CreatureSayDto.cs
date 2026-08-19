public class CreatureSayDto : IWireDto
{
    private int objectId = 0;
    private int chatType = 0;
    private string senderName;
    private string text;
    private CreatureMessage message;
    public CreatureMessage Message { get { return message; } }
    

    public void ReadFrom(PacketReader reader)
    {
        objectId = reader.ReadI();
        chatType = reader.ReadI();

        ChatTypeData data = ChatTypes.GetById(chatType);

        if(data != null)
        {
              senderName = reader.ReadOtherS();

              reader.ReadI();//High Five NPCString ID

              text = reader.ReadOtherS();

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
