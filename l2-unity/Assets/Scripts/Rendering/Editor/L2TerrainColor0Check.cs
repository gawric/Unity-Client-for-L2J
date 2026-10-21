using UnityEditor;
using UnityEngine;

/// <summary>
/// Forced inputs from the 01:00 path vertex. No camera, no texel, no live map.
/// </summary>
public static class L2TerrainColor0Check
{
    const float HsvX = 0.459757f;
    const float HsvY = 0.529181f;
    const float HsvZ = 0.574740f;
    const int AmbientR = 51;
    const int AmbientG = 98;
    const int AmbientB = 122;

    [MenuItem("L2/Check Terrain Color0")]
    public static void Run()
    {
        bool ok = true;
        ok &= ExpectColor(130, 54, 83, 98, "byte 130");
        ok &= ExpectColor(0, 25, 49, 61, "byte 0");

        L2TerrainSunIntensity.SlotPair(1f, 8, out int slotA, out int slotB, out float alpha);
        byte wrapped = L2TerrainSunIntensity.LerpByte(0, 156, alpha);
        if (slotA != 7 || slotB != 0 || wrapped != 130)
        {
            ok = false;
            // Debug.LogError(
                // "[L2] hour 1 pair " + slotA + "→" + slotB +
                // " alpha " + alpha.ToString("0.###") + " byte " + wrapped + ", expected 7→0 and 130");
        }

        L2TerrainSunIntensity.SlotPair(17f, 8, out slotA, out slotB, out alpha);
        byte evening = L2TerrainSunIntensity.LerpByte(213, 156, alpha);
        if (evening != 203)
        {
            ok = false;
            // Debug.LogError("[L2] hour 17 lerp byte " + evening + ", expected 203");
        }

        if (ok)
            Debug.Log("[L2] Terrain Color0 check passed: 130 → 54,83,98; 0 → 25,49,61; hour 1 wraps to 130; hour 17 stays 203.");
    }

    static bool ExpectColor(int intensity, int r, int g, int b, string label)
    {
        L2TerrainSunIntensity.Color0Bytes(
            intensity, HsvX, HsvY, HsvZ, AmbientR, AmbientG, AmbientB, out int gotR, out int gotG, out int gotB);
        if (gotR == r && gotG == g && gotB == b)
            return true;
        // Debug.LogError("[L2] " + label + " got " + gotR + "," + gotG + "," + gotB + " expected " + r + "," + g + "," + b);
        return false;
    }
}
