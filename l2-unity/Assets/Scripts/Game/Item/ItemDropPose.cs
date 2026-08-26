using UnityEngine;

/// <summary>
/// LIVE UGameEngine::OnDropItem pose for ground pickups.
/// DropType 1 (swords/daggers): Pitch -35498+0x4000, AnimMode 1 throw + Pitch spin.
/// DropType 2: Roll spin. DropType 3/4: club/staff pitch. Type 0: no stick.
/// Unreal rotator: 65536 = 360°. Unity euler via <see cref="VectorUtils.ConvertRotToUnity"/>.
/// </summary>
public static class ItemDropPose
{
    const int SlotYawStep = 1310720;

    public struct ThrowPose
    {
        public Vector3 LandEuler;
        public Vector3 TumbleAxis;
        public float TumbleDegPerSec;
        public bool StickOnLand;
        public bool TumbleInFlight;
        public float ArcHeightMeters;
    }

    public static bool TryBuildThrowPose(
        ItemEntity item,
        Abstractgrp grp,
        bool stickWeapon,
        Vector3 from,
        Vector3 landPos,
        out ThrowPose pose)
    {
        pose = default;
        if (item == null)
            return false;

        int dropType = grp != null ? grp.DropType : 0;
        Vector3 throwDir = landPos - from;

        pose.LandEuler = LandEulerFromDropType(dropType, throwDir, 0);
        pose.StickOnLand = stickWeapon &&
                           (dropType == 0 || (dropType >= 1 && dropType <= 4));
        pose.TumbleInFlight = pose.StickOnLand;
        pose.TumbleAxis = pose.TumbleInFlight
            ? TumbleWorldAxis(dropType, throwDir, 0)
            : Vector3.zero;
        pose.TumbleDegPerSec = pose.TumbleInFlight ? GetTumbleDegPerSec(0) : 0f;

        float horiz = Vector3.Distance(
            new Vector3(from.x, 0f, from.z),
            new Vector3(landPos.x, 0f, landPos.z));
        // OnDropItem: StartLoc.Z = landZ + Dist. Clamp so short drops still arc.
        pose.ArcHeightMeters = pose.StickOnLand
            ? Mathf.Clamp(horiz, 0.45f, 2.4f)
            : ItemDropPresentationIds.DropThrowArcHeightMeters;
        return true;
    }

    public static Vector3 LandEulerFromDropType(int dropType, Vector3 throwDir, int slot)
    {
        int pitch = 0;
        int yaw = dropType != 0 ? 0x4000 : 0;
        int roll = 0;
        int slotBias = (SlotYawStep * slot) / 360;

        switch (dropType)
        {
            case 1:
                pitch = -35498 - slotBias + 0x4000;
                break;
            case 2:
                pitch = -32768 - slotBias;
                break;
            case 3:
                pitch = -2731 - slotBias + 0x4000;
                break;
            case 4:
                pitch = slotBias + 0x4000;
                break;
        }

        Vector3 flat = new Vector3(throwDir.x, 0f, throwDir.z);
        if (flat.sqrMagnitude > 0.0001f)
        {
            float unityYaw = Quaternion.LookRotation(flat.normalized, Vector3.up).eulerAngles.y;
            int aimYaw = Mathf.RoundToInt(unityYaw * 65536f / 360f);
            yaw = dropType == 2 ? aimYaw + 0x4000 : aimYaw + 0x8000;
        }

        return VectorUtils.ConvertRotToUnity(new Vector3(pitch, yaw, roll));
    }

    /// <summary>
    /// Cartwheel in the throw's vertical plane (Unreal Pitch). DropType 2 rolls
    /// around the throw. Do not use actor-local X: Unity blades lie on local X,
    /// so Pitch there is a drill spin and looks like "no tumble".
    /// </summary>
    public static Vector3 TumbleWorldAxis(int dropType, Vector3 throwDir, int slot)
    {
        Vector3 flat = new Vector3(throwDir.x, 0f, throwDir.z);
        if (flat.sqrMagnitude < 0.0001f)
            flat = Vector3.forward;

        Vector3 axis = dropType == 2
            ? flat.normalized
            : Vector3.Cross(Vector3.up, flat.normalized);
        if (axis.sqrMagnitude < 1e-8f)
            axis = Vector3.right;
        axis.Normalize();
        if ((slot & 1) != 0)
            axis = -axis;
        return axis;
    }

    public static float GetTumbleDegPerSec(int slot)
    {
        // LIVE RotationRate 540016 uu/s ≈ 2966°/s. Our arc is 0.55s, so that is
        // ~4.5 flips (a smear). Two somersaults stay readable.
        float sign = (slot & 1) == 0 ? -1f : 1f;
        return sign * 720f * 1.3f * 1.3f / ItemDropPresentationIds.WeaponThrowArcSeconds;
    }
}
