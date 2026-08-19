using System.Collections.Generic;
using UnityEngine;

public class ShopPreviewInfoDto : IWireDto
{
    

    public void ReadFrom(PacketReader reader)
    {
        var totalSlots = reader.ReadI();
        var rear = reader.ReadI();
        var lear = reader.ReadI();
        var neck = reader.ReadI();
        var rfinger = reader.ReadI();
        var lfinge = reader.ReadI();
        var head = reader.ReadI();
        var rhand = reader.ReadI();
        var lhand = reader.ReadI();
        var gloves = reader.ReadI();
        var chest = reader.ReadI();
        var legs = reader.ReadI();
        var feet = reader.ReadI();
        var clock = reader.ReadI();
        var face = reader.ReadI();
        var hair = reader.ReadI();
        var hairall = reader.ReadI();
        var under = reader.ReadI();

    }
}
