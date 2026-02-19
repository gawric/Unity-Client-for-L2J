using System;
using UnityEngine;

public class NumberUtils {


    //private static float defaultMetrUnit = 0.0190f;
    private static float defaultMetrUnit = 0.0189f;
    public static float ScaleToUnity(float value) {
        return (float) value * defaultMetrUnit;
    }

    public static float ScaleToUnityPlus(float value)
    {
        //back up 35
        return value * (1f / 52.5f) + 0.25f;
    }

    public static float ScaleAnimSpeedToUnity(float value)
    {
        return value / 68f - 0.25f;
    }

    public static float ScaleAnimSpeedFigtherToUnity(float value)
    {
        return value / 68f;
    }

    public static float ScaleAnimSpeedMagicToUnity(float serverValue)
    {
   
        float calculatedMultiplier = serverValue * 0.0033f;
        return Mathf.Clamp(calculatedMultiplier, 0.25f, 0.8f);
    }

    public static float ScaleAnimSpeedL2jToUnity(float value)
    {
        return value / 278f;
    }

    public static float ScaleAnimWalkSpeedL2jToUnity(float value)
    {
        return value / 20f;
    }


    public static float FloorToNearest(float value, float step) {
        return step * Mathf.Floor(value / step);
    }

    public static float FromIntToFLoat(int value) {
        // Convert integer to byte array
        byte[] byteArray = BitConverter.GetBytes(value);

        // Convert byte array to float
        float floatValue = BitConverter.ToSingle(byteArray, 0);

        return floatValue;
    }
}
