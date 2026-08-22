using UnityEngine;

/// <summary>
/// Shared L2 nameplate world anchor (DrawTargetName):
/// Actor.Location + (0,0,CollisionHeight) ≈ capsule top. Prefer CC/Capsule; no bone bob.
/// </summary>
public static class L2NameplateAnchor
{
    public const float DefaultCollisionHeightMeters = 0.46f;

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

        // Deliberately NOT using cc/capsule dimensions anymore, even when present: the User_<Race>
        // prefabs (other players) and Player_<Race> prefabs (local player) ship with differently
        // tuned CharacterController sizes for the very same race (e.g. MFighter: Player_ capsule
        // top ~0.925m, User_ ~0.725m) - fine for movement/collision, but it made nameplates sit at
        // very different heights for identical races depending on whether the entity happened to be
        // the local player or someone else. CollisionHeight comes from the server (same field, same
        // per-race value for everyone), so anchoring on it instead keeps nameplate height consistent
        // no matter which prefab variant a given entity uses.
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
