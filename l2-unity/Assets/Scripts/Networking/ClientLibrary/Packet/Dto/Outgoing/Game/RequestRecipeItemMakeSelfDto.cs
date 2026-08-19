public sealed class RequestRecipeItemMakeSelfDto : IOutgoingDto
{
    public int RecipeId;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(RecipeId);
    }
}
