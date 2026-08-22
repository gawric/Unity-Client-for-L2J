[OutgoingCommandPacket(typeof(RequestRecipeItemMakeInfoCommand))]
public sealed class RequestRecipeItemMakeInfo : OutgoingWirePacket<RequestRecipeItemMakeInfoDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestRecipeItemMakeInfo;

    public RequestRecipeItemMakeInfo(RequestRecipeItemMakeInfoCommand command) : this(command.RecipeId) { }

    public RequestRecipeItemMakeInfo(int recipeId)
    {
        Dto.RecipeId = recipeId;
    }
}
