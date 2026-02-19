using UnityEngine;

public class ArmorClassifier 
{
   public static ArmorType GetArmorType(string armor_type)
   {
        switch (armor_type.ToLower()) 
        {
            case "light":
                return ArmorType.light;
            case "heavy":
                return ArmorType.heavy;
            case "none":
                return ArmorType.none;
            case "magic":
                return ArmorType.magic;
            default:
                return ArmorType.none; 
        }
    }
}
