using System;
using System.Collections.Generic;

/// <summary>
/// Locomotion family played via CrossFadeInFixedTime (not bool graph).
/// </summary>
public enum PlayerLocomotionFamily
{
    Wait,
    Walk,
    Run,
    AtkWait
}

/// <summary>
/// Shared fade tuning for player CrossFadeInFixedTime (locomotion + basic atk).
/// </summary>
public static class LocomotionCrossFadeSettings
{
    public const float DefaultFixedDuration = 0.15f;
    public static float FixedDuration = DefaultFixedDuration;
}

/// <summary>
/// Maps wait_/walk_/run_/atkwait_ + weapon suffix → Animator state name.
/// </summary>
public sealed class PlayerLocomotionAnim : PlayerAnimStateMapBase
{
    public static readonly PlayerLocomotionAnim Instance = new PlayerLocomotionAnim();

    static readonly string[] Prefixes =
    {
        "atkwait_", // before wait_
        "wait_",
        "walk_",
        "run_"
    };

    protected override IReadOnlyList<string> PrefixesLongestFirst => Prefixes;

    public static string FamilyPrefix(PlayerLocomotionFamily family)
    {
        switch (family)
        {
            case PlayerLocomotionFamily.Wait: return "wait_";
            case PlayerLocomotionFamily.Walk: return "walk_";
            case PlayerLocomotionFamily.Run: return "run_";
            case PlayerLocomotionFamily.AtkWait: return "atkwait_";
            default: return "wait_";
        }
    }

    public static string ToSuffix(PlayerAnimWeapon weapon) => ToWeaponSuffix(weapon);

    public static bool TryFromSuffix(string weaponSuffix, out PlayerAnimWeapon weapon) =>
        TryParseWeaponSuffix(weaponSuffix, out weapon);

    public static string ToStateName(PlayerLocomotionFamily family, PlayerAnimWeapon weapon) =>
        ComposeStateName(FamilyPrefix(family), weapon);

    public static bool TryParseFamilyPrefix(string animName, out PlayerLocomotionFamily family, out string suffix)
    {
        family = default;
        suffix = null;
        if (!Instance.TryResolveStateName(animName, out _, out string matchedPrefix))
        {
            return false;
        }

        if (!TryFamilyFromPrefix(matchedPrefix, out family))
        {
            return false;
        }

        suffix = animName.Substring(matchedPrefix.Length);
        return true;
    }

    /// <summary>
    /// Full name from PlayAnimation (e.g. wait_1HS, walk_dual, run_bow, atkwait_1HS).
    /// </summary>
    public static bool TryResolve(string animName, out string stateName, out PlayerLocomotionFamily family)
    {
        stateName = null;
        family = default;
        if (!Instance.TryResolveStateName(animName, out stateName, out string matchedPrefix))
        {
            return false;
        }

        return TryFamilyFromPrefix(matchedPrefix, out family);
    }

    static bool TryFamilyFromPrefix(string prefix, out PlayerLocomotionFamily family)
    {
        switch (prefix)
        {
            case "atkwait_":
                family = PlayerLocomotionFamily.AtkWait;
                return true;
            case "wait_":
                family = PlayerLocomotionFamily.Wait;
                return true;
            case "walk_":
                family = PlayerLocomotionFamily.Walk;
                return true;
            case "run_":
                family = PlayerLocomotionFamily.Run;
                return true;
            default:
                family = default;
                return false;
        }
    }
}
