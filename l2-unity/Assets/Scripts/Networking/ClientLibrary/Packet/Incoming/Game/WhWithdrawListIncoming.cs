using UnityEngine;

[IncomingGamePacket(GameServerPacketType.WhWithdrawList)]
public sealed class WhWithdrawListIncoming : IncomingWirePacket<WarehouseWithdrawListDto>
{
    public override void Apply(WarehouseWithdrawListDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            UserInfoDto info = StorageNpc.getInstance().GetFirstUser();
            IncomingPacketActions.Dealer.SetWindowName("Personal storage");
            IncomingPacketActions.Dealer.SetHeaderNameSellPanel("Items in Warehouse");
            IncomingPacketActions.Dealer.SetHeaderNameBuyPanel("Items taken away");
            IncomingPacketActions.Dealer.SetProductType(ProductType.WHWithdrawList);
            IncomingPacketActions.Dealer.UpdateBuyData(packet.Products, true, -1);
            IncomingPacketActions.Dealer.UpdateDataForm(packet.CurrentMoney, info.PlayerInfoInterlude.Stats.WeightPercent(), info.PlayerInfoInterlude.Stats.CurrWeight, info.PlayerInfoInterlude.Stats.MaxWeight);
            IncomingPacketActions.Dealer.ShowWindow();
        });
    }
}
