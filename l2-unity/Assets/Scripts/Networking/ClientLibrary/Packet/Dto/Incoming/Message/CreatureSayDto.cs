public class CreatureSayDto : IWireDto
{
    private int objectId = 0;
    private int chatType = 0;
    private string senderName;
    private string text;
    private CreatureMessage message;
    public CreatureMessage Message { get { return message; } }

    public static CreatureSayDto From(int objectId, int chatType, string senderName, string text)
    {
        ChatTypeData data = ChatTypes.GetById(chatType);
        CreatureMessage message = null;
        if (data != null)
        {
            int dataType = data.Type;
            message = new CreatureMessage(
                dataType == 10 || dataType == 18 ? "Announcements" : senderName,
                text,
                data);
        }

        return new CreatureSayDto
        {
            objectId = objectId,
            chatType = chatType,
            senderName = senderName,
            text = text,
            message = message,
        };
    }
    

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
