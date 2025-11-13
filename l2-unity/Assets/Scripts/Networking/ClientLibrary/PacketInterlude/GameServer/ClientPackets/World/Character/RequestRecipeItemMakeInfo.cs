using UnityEngine;

public class RequestRecipeItemMakeInfo : ClientPacket
{
    public RequestRecipeItemMakeInfo(int recipeId) : base((byte)GameInterludeClientPacketType.RequestRecipeItemMakeInfo)
    {
        WriteI(recipeId);
        BuildPacket();
    }
}
