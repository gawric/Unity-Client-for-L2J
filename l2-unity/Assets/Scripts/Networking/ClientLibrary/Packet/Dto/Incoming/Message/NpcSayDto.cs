using System.Security.Principal;
using UnityEngine;

public class NpcSayDto : IWireDto
{
    private int _objectId;
    private int _textType;
    private int _npcId;
    private string  _textMessage;
    private CreatureMessage message;

    public int ObjectId { get => _objectId; }
    public int TextType { get => _textType; }
    public int NpcId { get => _npcId; }
    public string Text { get => _textMessage; }
    public CreatureMessage NpcMessage { get => message; }
    

    public void ReadFrom(PacketReader reader)
    {
        //Debug.Log("Пришел пакет NpcSay 1" + " text " + _textMessage);
        _objectId = reader.ReadI();
        _textType = reader.ReadI(); //chatType
        _npcId = reader.ReadI() - 1000000; // npctype id (-1000000)
        _textMessage = reader.ReadOtherS();
        NpcName npcName = NpcNameTable.Instance.GetNpcName(_npcId);
        string senderName = "";
        if(npcName != null)
        {
            senderName = npcName.Name;
        }
        CreateMessage(_textType, _textMessage, senderName);
        //Debug.Log("Пришел пакет NpcSay " + senderName + " text " + _textMessage);
    }

    private void CreateMessage(int chatType , string text , string senderName)
    {
        ChatTypeData data = ChatTypes.GetById(chatType);

        if (data != null)
        {
            int dataType = data.Type;

            if (dataType == 10 || dataType == 18)
            {
                message = new CreatureMessage("Announcements", text , data);
            }
            else
            {
                message = new CreatureMessage(senderName, text , data);
            }
        }
    }
}
