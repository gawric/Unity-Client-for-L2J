using UnityEngine;

public class RequestRecipeItemMakeSelf : ClientPacket
{
    public RequestRecipeItemMakeSelf(int recipeId) : base((byte)GameInterludeClientPacketType.RequestRecipeItemMakeSelf)
    {
        WriteI(recipeId);
        BuildPacket();
    }
}
