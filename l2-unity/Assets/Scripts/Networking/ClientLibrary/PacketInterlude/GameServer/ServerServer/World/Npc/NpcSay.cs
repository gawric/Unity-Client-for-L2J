using System.Security.Principal;
using UnityEngine;

public class NpcSay : ServerPacket
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
    public NpcSay(byte[] d) : base(d)
    {

        Parse();
    }

    public override void Parse()
    {
        //Debug.Log("Пришел пакет NpcSay 1" + " text " + _textMessage);
        _objectId = ReadI();
        _textType = ReadI(); //chatType
        _npcId = ReadI() - 1000000; // npctype id (-1000000)
        _textMessage = ReadOtherS();
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
