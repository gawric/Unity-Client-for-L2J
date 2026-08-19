using UnityEngine;

[IncomingGamePacket(GameServerPacketType.WhDepositList)]
public sealed class WhDepositListIncoming : IncomingWirePacket<WarehouseDepositListDto>
{
    public override void Apply(WarehouseDepositListDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            UserInfoDto info = StorageNpc.getInstance().GetFirstUser();
            IncomingPacketActions.Dealer.SetWindowName("Personal storage");
            IncomingPacketActions.Dealer.SetHeaderNameSellPanel("Inventory");
            IncomingPacketActions.Dealer.SetHeaderNameBuyPanel("Items in stock");
            IncomingPacketActions.Dealer.SetProductType(ProductType.WHDepositList);
            IncomingPacketActions.Dealer.UpdateBuyData(packet.Products, true, -1);
            IncomingPacketActions.Dealer.UpdateDataForm(packet.CurrentMoney, info.PlayerInfoInterlude.Stats.WeightPercent(), info.PlayerInfoInterlude.Stats.CurrWeight, info.PlayerInfoInterlude.Stats.MaxWeight);
            IncomingPacketActions.Dealer.ShowWindow();
        });
    }
}
