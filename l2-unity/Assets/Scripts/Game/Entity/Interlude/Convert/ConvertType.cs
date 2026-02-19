using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConvertType
{
    public static int GetType(string face)
    {
        if (face.Equals("Type A"))
        {
            return (int)EnumType.TypeA;
        }
        else if (face.Equals("Type B"))
        {
            return (int)EnumType.TypeB;
        }
        else if (face.Equals("Type C"))
        {
            return (int)EnumType.TypeC;
        }
        return 0;
    }

    public static byte ConvertTypeToByte(string face)
    {
        if (face.Equals("Type A"))
        {
            return (byte)EnumType.TypeA;
        }
        else if (face.Equals("Type B"))
        {
            return (byte)EnumType.TypeB;
        }
        else if (face.Equals("Type C"))
        {
            return (byte)EnumType.TypeC;
        }
        return 0;
    }

    public static int GetIntSex(string male)
    {
        if (male.Equals("Male"))
        {
            return 0;
        }else if (male.Equals("Female"))
        {
            return 1;
        }
        return 0;
    }
}
