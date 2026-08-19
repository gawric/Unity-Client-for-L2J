using UnityEngine;

[IncomingGamePacket(GameServerPacketType.ShopPreviewList)]
public sealed class ShopPreviewListIncoming : IncomingWirePacket<ShopPreviewListDto>
{
    public override void Apply(ShopPreviewListDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            UserInfoDto info = StorageNpc.getInstance().GetFirstUser();
            IncomingPacketActions.Dealer.SetHeaderNameSellPanel("Attempt");
            IncomingPacketActions.Dealer.SetHeaderNameBuyPanel("Selection list");
            IncomingPacketActions.Dealer.SetProductType(ProductType.WEAR);
            IncomingPacketActions.Dealer.UpdateBuyData(packet.Products, false, packet.ListID);
            IncomingPacketActions.Dealer.UpdateDataForm(packet.CurrentMoney, info.PlayerInfoInterlude.Stats.WeightPercent(), info.PlayerInfoInterlude.Stats.CurrWeight, info.PlayerInfoInterlude.Stats.MaxWeight);
            IncomingPacketActions.Dealer.ShowWindowToCenter();
        });
    }
}
