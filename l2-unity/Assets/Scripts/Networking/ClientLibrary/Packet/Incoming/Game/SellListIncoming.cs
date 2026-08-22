using UnityEngine;

[IncomingGamePacket(GameServerPacketType.SellList)]
public sealed class SellListIncoming : IncomingWirePacket<SellListDto>
{
    public override void Apply(SellListDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            UserInfoDto info = StorageNpc.getInstance().GetFirstUser();
            IncomingPacketActions.Dealer.SetWindowName("Shop");
            IncomingPacketActions.Dealer.SetHeaderNameSellPanel("Inventory");
            IncomingPacketActions.Dealer.SetHeaderNameBuyPanel("Sell");
            IncomingPacketActions.Dealer.SetProductType(ProductType.SELL);
            IncomingPacketActions.Dealer.UpdateBuyData(packet.Products, true, packet.ListID);
            IncomingPacketActions.Dealer.UpdateDataForm(packet.CurrentMoney, info.PlayerInfoInterlude.Stats.WeightPercent(), info.PlayerInfoInterlude.Stats.CurrWeight, info.PlayerInfoInterlude.Stats.MaxWeight);
            IncomingPacketActions.Dealer.ShowWindow();
        });
    }
}
