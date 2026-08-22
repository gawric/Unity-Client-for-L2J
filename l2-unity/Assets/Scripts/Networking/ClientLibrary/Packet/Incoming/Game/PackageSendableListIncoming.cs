using UnityEngine;

[IncomingGamePacket(GameServerPacketType.PackageSendableList)]
public sealed class PackageSendableListIncoming : IncomingWirePacket<PackageSendableListDto>
{
    public override void Apply(PackageSendableListDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            UserInfoDto info = StorageNpc.getInstance().GetFirstUser();
            IncomingPacketActions.Dealer.SetWindowName("Send a parcel");
            IncomingPacketActions.Dealer.SetHeaderNameSellPanel("Inventory");
            IncomingPacketActions.Dealer.SetHeaderNameBuyPanel("Shipping list");
            IncomingPacketActions.Dealer.SetProductType(ProductType.PackageSendableList);
            IncomingPacketActions.Dealer.UpdateBuyData(packet.Items, true, packet.PlayerObject);
            IncomingPacketActions.Dealer.UpdateDataForm(packet.CurrentMoney, info.PlayerInfoInterlude.Stats.WeightPercent(), info.PlayerInfoInterlude.Stats.CurrWeight, info.PlayerInfoInterlude.Stats.MaxWeight);
            IncomingPacketActions.Dealer.ShowWindow();
        });
    }
}
