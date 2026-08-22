[OutgoingCommandPacket(typeof(RequestRecipeBookDestroyCommand))]
public sealed class RequestRecipeBookDestroy : OutgoingWirePacket<RequestRecipeBookDestroyDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestRecipeBookDestroy;

    public RequestRecipeBookDestroy(RequestRecipeBookDestroyCommand command) : this(command.RecipeId) { }

    public RequestRecipeBookDestroy(int recipeId)
    {
        Dto.RecipeId = recipeId;
    }
}
