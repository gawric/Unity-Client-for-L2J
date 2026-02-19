using UnityEngine;

public class ChatTypeData
{
    public int Id;
    public int Type;
    public string Text;
    public string Color;
		//Test 1
    public ChatTypeData(int id, int type, string text, string color)
    {
        Id = id;
        Type = type;
        Text = text;
        Color = color;
    }
}