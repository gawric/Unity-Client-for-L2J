using UnityEngine;

[IncomingGamePacket(GameServerPacketType.BuyListSeed)]
public sealed class BuyListSeedIncoming : IncomingWirePacket<BuyListSeedDto>
{
    public override void Apply(BuyListSeedDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            UserInfoDto info = StorageNpc.getInstance().GetFirstUser();
            IncomingPacketActions.Dealer.SetWindowName("Estate");
            IncomingPacketActions.Dealer.SetHeaderNameSellPanel("Sale");
            IncomingPacketActions.Dealer.SetHeaderNameBuyPanel("Purchase");
            IncomingPacketActions.Dealer.SetProductType(ProductType.BUY_SEED);
            IncomingPacketActions.Dealer.UpdateBuyData(packet.List, true, packet.ManorId);
            IncomingPacketActions.Dealer.UpdateDataForm(packet.CurrentMoney, info.PlayerInfoInterlude.Stats.WeightPercent(), info.PlayerInfoInterlude.Stats.CurrWeight, info.PlayerInfoInterlude.Stats.MaxWeight);
            IncomingPacketActions.Dealer.ShowWindow();
        });
    }
}
