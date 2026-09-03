using UnityEngine;

/// <summary>
/// Shared L2 nameplate world anchor (DrawTargetName):
/// Actor.Location + (0,0,CollisionHeight) ≈ capsule top. Prefer CC/Capsule; no bone bob.
/// </summary>
public static class L2NameplateAnchor
{
    public const float DefaultCollisionHeightMeters = 0.46f;
    // NameplatesManager._headHeightOffset default. Spawn TargetOverHead uses this too.
    public const float DefaultHeadHeightOffsetMeters = -0.12f;

    /// <summary>
    /// World position just above the capsule top (+ <paramref name="headHeightOffset"/>).
    /// </summary>
    public static Vector3 GetHeadWorldPos(
        Transform target,
        CharacterController cc,
        CapsuleCollider capsule,
        float collisionHeight,
        float headHeightOffset)
    {
        if (target == null)
        {
            return Vector3.zero;
        }

        if (cc != null)
        {
            Vector3 localTop = cc.center + Vector3.up * (cc.height * 0.5f);
            return target.TransformPoint(localTop) + Vector3.up * headHeightOffset;
        }

        if (capsule != null)
        {
            Vector3 localTop = capsule.center + Vector3.up * (capsule.height * 0.5f);
            return target.TransformPoint(localTop) + Vector3.up * headHeightOffset;
        }

        // Feet GO + 2×CH: UE Location is capsule center (= feet+CH), name = Loc+CH.
        return target.position + Vector3.up * HeightFromFeet(collisionHeight, headHeightOffset);
    }

    /// <summary>
    /// Resolve CC / Capsule on <paramref name="target"/> then <see cref="GetHeadWorldPos"/>.
    /// </summary>
    public static Vector3 GetHeadWorldPos(
        Transform target,
        float collisionHeight,
        float headHeightOffset)
    {
        if (target == null)
        {
            return Vector3.zero;
        }

        CharacterController cc = target.GetComponent<CharacterController>();
        if (cc == null)
        {
            cc = target.GetComponentInChildren<CharacterController>();
        }

        CapsuleCollider capsule = null;
        if (cc == null)
        {
            capsule = target.GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                capsule = target.GetComponentInChildren<CapsuleCollider>();
            }
        }

        return GetHeadWorldPos(target, cc, capsule, collisionHeight, headHeightOffset);
    }

    /// <summary>
    /// Half-height in Unity meters. npcgrp is already /52.5; UserInfoDto often raw UU.
    /// </summary>
    public static float CollisionHeightToUnityMeters(float collisionHeight)
    {
        if (collisionHeight <= 0.0001f)
        {
            return DefaultCollisionHeightMeters;
        }

        // Raw Interlude UU half-heights are typically ~3..40; converted meters are usually under ~1.
        if (collisionHeight > 2.5f)
        {
            return collisionHeight / 52.5f;
        }

        return collisionHeight;
    }

    public static float HeightFromFeet(float collisionHeight, float headHeightOffset)
    {
        float ch = CollisionHeightToUnityMeters(collisionHeight);
        return 2f * ch + headHeightOffset;
    }
}
