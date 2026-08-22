using UnityEngine;

[IncomingGamePacket(GameServerPacketType.RecipeBookItemList)]
public sealed class RecipeBookItemListIncoming : IncomingWirePacket<RecipeBookItemListDto>
{
    public override void Apply(RecipeBookItemListDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            IncomingPacketActions.RecipeBook.AddData(packet);
            IncomingPacketActions.RecipeBook.ShowWindow();
        });
    }
}
