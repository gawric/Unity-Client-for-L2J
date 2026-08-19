[OutgoingCommandPacket(typeof(RequestRecipeItemMakeSelfCommand))]
public sealed class RequestRecipeItemMakeSelf : OutgoingWirePacket<RequestRecipeItemMakeSelfDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestRecipeItemMakeSelf;

    public RequestRecipeItemMakeSelf(RequestRecipeItemMakeSelfCommand command) : this(command.RecipeId) { }

    public RequestRecipeItemMakeSelf(int recipeId)
    {
        Dto.RecipeId = recipeId;
    }
}
