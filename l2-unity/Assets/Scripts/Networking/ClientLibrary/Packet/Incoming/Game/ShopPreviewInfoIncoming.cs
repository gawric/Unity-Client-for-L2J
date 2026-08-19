using UnityEngine;

[IncomingGamePacket(GameServerPacketType.ShopPreviewInfo)]
public sealed class ShopPreviewInfoIncoming : IncomingWirePacket<ShopPreviewInfoDto>
{
    public override void Apply(ShopPreviewInfoDto packet)
    {
        Debug.Log("There is no implementation of this package.> OnShopPreviewInfo");
    }
}
