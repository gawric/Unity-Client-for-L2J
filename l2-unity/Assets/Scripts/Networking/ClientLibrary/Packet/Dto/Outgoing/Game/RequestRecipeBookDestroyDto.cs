public sealed class RequestRecipeBookDestroyDto : IOutgoingDto
{
    public int RecipeId;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(RecipeId);
    }
}
