using System.Collections.Generic;
using UnityEngine;

public class ShopPreviewInfo : ServerPacket
{
    public ShopPreviewInfo(byte[] d) : base(d)
    {

        Parse();
    }

    public override void Parse()
    {
        var totalSlots = ReadI();
        var rear = ReadI();
        var lear = ReadI();
        var neck = ReadI();
        var rfinger = ReadI();
        var lfinge = ReadI();
        var head = ReadI();
        var rhand = ReadI();
        var lhand = ReadI();
        var gloves = ReadI();
        var chest = ReadI();
        var legs = ReadI();
        var feet = ReadI();
        var clock = ReadI();
        var face = ReadI();
        var hair = ReadI();
        var hairall = ReadI();
        var under = ReadI();

    }
}
