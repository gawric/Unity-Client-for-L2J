/// <summary>
/// Summon npc ids. Wolf 12077 still falls back to the wild wolf mesh when its own row is missing.
/// </summary>
public static class PetVisuals
{
    public static int VisualNpcId(int petNpcId)
    {
        if (petNpcId == 12077)
            return 20120;
        return petNpcId;
    }

    public static bool IsPet(EntityIdentity identity)
    {
        if (identity == null)
            return false;
        if (identity.Owned)
            return true;
        if (IsSummonNpc(identity.NpcId))
            return true;
        return VisualNpcId(identity.NpcId) != identity.NpcId;
    }

    public static bool IsSummonNpc(int npcId)
    {
        switch (npcId)
        {
            case 12077:
            case 12311:
            case 12312:
            case 12313:
            case 12526:
            case 12527:
            case 12528:
            case 12621:
            case 12780:
            case 12781:
            case 12782:
                return true;
            default:
                return false;
        }
    }
}
