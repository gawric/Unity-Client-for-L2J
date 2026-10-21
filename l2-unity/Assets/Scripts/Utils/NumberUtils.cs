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

    // Interlude sequence rate is 30 fps (AnimTween). Imported clips keep the same
    // keys on a 24 fps timeline: stone golem run is 91 frames, Unity stop time
    // (91-1)/24 = 3.75s. L2 plays those frames at 30 * PlayAnim rate.
    // Live Crasher PlayAnim rate is 1.10 (NonAttackSpeedRate; the AnimRate slot is 0).
    // Animator speed = 1.10 * 30/24. 24/30 ≈ 0.8 is the raw fps ratio, not the
    // parameter: the longer Unity timeline has to be played faster to finish
    // the cycle in the same 91/30/1.10 seconds.
    const float L2SequenceFps = 30f;
    const float UnityClipFps = 24f;
    const float L2NpcMovePlayRate = 1.10f;

    public static float NpcLocomotionAnimRate()
    {
        return L2NpcMovePlayRate * L2SequenceFps / UnityClipFps;
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
