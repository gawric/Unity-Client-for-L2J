/// <summary>
/// Pet template ids from aCis are not rows in Npcgrp_Classic.
/// The summon uses the already imported mesh of a lookalike npc.
/// </summary>
public static class PetVisuals
{
    public static int VisualNpcId(int petNpcId)
    {
        if (petNpcId == 12077)
            return 20120;
        return petNpcId;
    }
}
