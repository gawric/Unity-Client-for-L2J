using System.Drawing;
using System;
using UnityEngine;
using System.Xml.Serialization;

public class MyTargetSelect : ServerPacket
{
    private int _objectId;
    private string _color;
  

    public int ObjectId { get => _objectId; }
    public string Color { get => _color; }
    public MyTargetSelect(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        _objectId = ReadI();
        _color = ParceColor(ReadSh());
        ReadI();
    }

    private string ParceColor(int color)
    {
        if(color == 11)
        {
            return "#1410b7";
        }else if(color == 0)
        {
            return "#ffffff";
        }
        else if (color == 5)
        {
            return "#a2fbab";
        }
        else if (color == 7)
        {
            return "#a2a5fc";
        }
        return "#ffffff";
    }


}
