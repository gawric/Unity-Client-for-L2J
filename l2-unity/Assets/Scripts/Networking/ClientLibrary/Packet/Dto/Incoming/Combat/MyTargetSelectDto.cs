using System.Drawing;
using System;
using UnityEngine;
using System.Xml.Serialization;

public class MyTargetSelectDto : IWireDto
{
    private int _objectId;
    private string _color;
  

    public int ObjectId { get => _objectId; }
    public string Color { get => _color; }
    

    public void ReadFrom(PacketReader reader)
    {
        _objectId = reader.ReadI();
        _color = ParceColor(reader.ReadSh());
        reader.ReadI();
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
