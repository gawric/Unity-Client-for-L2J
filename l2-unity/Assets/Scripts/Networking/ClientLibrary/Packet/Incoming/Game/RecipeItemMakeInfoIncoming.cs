using UnityEngine;

[IncomingGamePacket(GameServerPacketType.RecipeItemMakeInfo)]
public sealed class RecipeItemMakeInfoIncoming : IncomingWirePacket<RecipeItemMakeInfoDto>
{
    public override void Apply(RecipeItemMakeInfoDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            IncomingPacketActions.RecipeBook.HideWindow();
            IncomingPacketActions.Crafting.AddData(packet);
            IncomingPacketActions.Crafting.ShowWindow();
        });
    }
}
