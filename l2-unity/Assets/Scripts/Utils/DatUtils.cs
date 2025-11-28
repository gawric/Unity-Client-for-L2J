using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows;

public class DatUtils {

    public static string CleanupString(string name) {
        return name.Replace("[", string.Empty).Replace("]", string.Empty);
    }

    public static string CleanupStringNew(string name)
    {
        return name.Replace("{", string.Empty).Replace("}", string.Empty);
    }

    public static string CleanupStringNewN(string name)
    {
        return name.Replace("\\n", string.Empty).Replace("\\", string.Empty);
    }

    public static string CleanupStringOldData(string name)
    {
        return name.Replace("a,", string.Empty).Replace("\\0", string.Empty);
    }

    public static string[] ParseArray(string value) {
        return SplitJSON(value);
    }

    public static List<List<string>> ParseMultiArray(string value)
    {
        return SplitJSONMultiArr(value);
    }

    public static string ParseIcon(string value) {
        return SplitJSON(value)[0];
    }

    public static string[] ParseOtherIcon(string value)
    {
        return SplitJSON(value);
    }

    public static string[] SplitJSON(string value) {
        return value.Replace("{", string.Empty).Replace("}", string.Empty).Replace("[", string.Empty).Replace("]", string.Empty).Split(";");
    }

    public static List<List<string>> SplitJSONMultiArr(string value)
    {
        // Remove outermost curly braces
        string inner = value.Trim().Substring(1, value.Length - 2);

        // Split into top-level arrays (separated by };{)
        string[] topLevelArrays = inner.Split(new[] { "};{" }, StringSplitOptions.None);

        // Create a list to hold our results
        var result = new List<List<string>>();

        foreach (var array in topLevelArrays)
        {
            // Process each array
            string cleanedArray = array.Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", "");
            string[] elements = cleanedArray.Split(';');

            // Add the elements as a new list to our result
            result.Add(elements.ToList());
        }

        return result;
    }


    public static bool ParseBaseAbstractItemGrpDat(Abstractgrp abstractgrp, string key, string value) {

        switch (key) {
            case "object_id":
                abstractgrp.ObjectId = int.Parse(value);
                break;
            case "drop_texture": //{{[dropitems.drop_mfighter_m001_t02_u_m00];{[MFighter.MFighter_m001_t02_u]}}}
                string[] modTex = DatUtils.ParseArray(value);
                abstractgrp.DropTexture = modTex[1];
                abstractgrp.DropModel = modTex[0];
                break;
            case "icon": // {[icon.armor_t02_u_i00];[None];[None];[None];[None]}
                if(abstractgrp.ObjectId == 356)
                {
                    Debug.Log("");
                }
                var allIcon = DatUtils.ParseOtherIcon(value);
                abstractgrp.Icon = allIcon[0];
                if (allIcon.Length > 1) abstractgrp.SetOtherIcon(allIcon);
                break;
            case "weight":
                abstractgrp.Weight = int.Parse(value);
                break;
            case "material_type":
                abstractgrp.Material = ItemMaterialParser.Parse(value);
                break;
            case "crystal_type": //crystal_type=d
                abstractgrp.Grade = ItemGradeParser.Parse(value);
                break;
            case "drop_sound": // [ItemSound.itemdrop_dagger]
                abstractgrp.DropSound = DatUtils.CleanupString(value);
                break;
            case "crystallizable":
                abstractgrp.Crystallizable = value == "1";
                break;
            case "equip_sound":
                abstractgrp.EquipSound = DatUtils.CleanupString(value);
                break;
            case "inventory_type":
                abstractgrp.InventoryType = value;
                break;
            default:
                return false;
        }


        return true;
    }
}
