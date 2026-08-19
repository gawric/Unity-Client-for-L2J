using UnityEngine;

[IncomingGamePacket(GameServerPacketType.BuyList)]
public sealed class BuyListIncoming : IncomingWirePacket<BuyListDto>
{
    public override void Apply(BuyListDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            UserInfoDto info = StorageNpc.getInstance().GetFirstUser();
            IncomingPacketActions.Dealer.SetWindowName("Shop");
            IncomingPacketActions.Dealer.SetHeaderNameSellPanel("Sell");
            IncomingPacketActions.Dealer.SetHeaderNameBuyPanel("Buy");
            IncomingPacketActions.Dealer.SetProductType(ProductType.BUY);
            IncomingPacketActions.Dealer.UpdateBuyData(packet.Products, false, packet.ListID);
            IncomingPacketActions.Dealer.UpdateDataForm(packet.CurrentMoney, info.PlayerInfoInterlude.Stats.WeightPercent(), info.PlayerInfoInterlude.Stats.CurrWeight, info.PlayerInfoInterlude.Stats.MaxWeight);
            IncomingPacketActions.Dealer.ShowWindowToCenter();
        });
    }
}
