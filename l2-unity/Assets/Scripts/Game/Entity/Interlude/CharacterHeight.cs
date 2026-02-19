using UnityEngine;

public class CharacterHeight
{
    public static float GetHeight(CharacterRaceAnimation race)
    {
        if (CharacterRaceAnimation.FDwarf.Equals(race) | CharacterRaceAnimation.MDwarf.Equals(race))
        {
            return 0.7f;
        }
        else if (CharacterRaceAnimation.FDarkElf.Equals(race))
        {
            return 0.92f;
        }
        //default
        return 0.92f;
    }
}
