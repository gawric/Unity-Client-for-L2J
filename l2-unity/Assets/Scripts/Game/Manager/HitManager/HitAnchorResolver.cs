using UnityEngine;

public static class HitAnchorResolver
{
    private static readonly string[] HitAnchorBoneNames =
    {
        "Bip01 L Clavicle",
        "Bip01 R Clavicle",
        "Bip01 L UpperArm",
        "Bip01 R UpperArm",
        "Bip01_L_Clavicle",
        "Bip01_R_Clavicle",
        "Bip01_L_UpperArm",
        "Bip01_R_UpperArm",
        "Bip01 Spine2",
        "Bip01 Spine1",
        "Bip01 Spine"
    };

    public static Transform ResolveHitAnchor(Entity targetEntity, Transform fallbackRoot)
    {
        if (targetEntity == null) return fallbackRoot;
        Gear gear = targetEntity.Gear;
        if (gear == null) return fallbackRoot;

        for (int i = 0; i < HitAnchorBoneNames.Length; i++)
        {
            Transform bone = gear.FindRecursiveBone(HitAnchorBoneNames[i]);
            if (bone != null)
            {
                return bone;
            }
        }

        return fallbackRoot;
    }
}
