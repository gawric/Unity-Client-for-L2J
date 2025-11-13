using UnityEngine;

public class RequestRecipeBookDestroy : ClientPacket
{
    public RequestRecipeBookDestroy(int recipeId) : base((byte)GameInterludeClientPacketType.RequestRecipeBookDestroy)
    {
        WriteI(recipeId);
        BuildPacket();
    }
}
