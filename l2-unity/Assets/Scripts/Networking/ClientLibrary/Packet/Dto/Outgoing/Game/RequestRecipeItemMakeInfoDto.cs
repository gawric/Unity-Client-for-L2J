public sealed class RequestRecipeItemMakeInfoDto : IOutgoingDto
{
    public int RecipeId;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(RecipeId);
    }
}
