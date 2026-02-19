using System;
using System.Collections.Generic;
using UnityEngine;

public class ArrayUtils
{
    public static bool IsValidIndexList<T>(List<T> array, int index)
    {
        return index >= 0 && index < array.Count;
    }

    public static bool IsValidIndexArray(Array array, int index)
    {
        return index >= 0 && index < array.Length;
    }

}
