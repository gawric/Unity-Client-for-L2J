using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static L2Slot;

public class ToolTipsUtils
{


    public static Color GetColorPrice(string price)
    {
        if (price.Length == 6)
        {
            return new Color(255f / 255f, 126f / 255f, 255f / 255f);
            //return new Color(0, 251, 255);
        }
        else if (price.Length == 5)
        {
            return new Color(0, 251, 255);
        }
        return new Color(169, 180, 196);
    }
    public static string ConvertToPrice(int wholeNumber)
    {
        string formattedNumber = wholeNumber.ToString("N0", CultureInfo.InvariantCulture);
        return formattedNumber.Replace('.', ',');
    }

    public static string ConvertToPrice(long wholeNumber)
    {
        string formattedNumber = wholeNumber.ToString("N0", CultureInfo.InvariantCulture);
        return formattedNumber.Replace('.', ',');
    }


    public static string ConvertPriceToNormal(string wholeNumber)
    {
        return wholeNumber.Replace(",", "");
    }

    public static string ConvertNumberToNormal(string wholeNumber)
    {
        return wholeNumber.Replace(",", ".");
    }

    public static VisualElement CloneOne(VisualTreeAsset vta)
    {
        var e = vta.CloneTree();
        return e.Children().First();
    }

    public static string[] GetUniquePosition(VisualElement ve)
    {
        return ve.name.Split('_');
    }

    public static SlotType DetectedClickPanel(int type)
    {
        switch (type)
        {
            case (int)SlotType.PriceBuy:
                return SlotType.PriceBuy;
            case (int)SlotType.PriceSell:
                return SlotType.PriceSell;
            case (int)SlotType.BuffPanel:
                return SlotType.BuffPanel;
            case (int)SlotType.Recipe:
                return SlotType.Recipe;
            case (int)SlotType.Enchant:
                return SlotType.Enchant;
            case (int)SlotType.Skill:
                return SlotType.Skill;
            case (int)SlotType.SkillBar:
                return SlotType.SkillBar;
            default:
                return SlotType.Other;
        }
    }
}
