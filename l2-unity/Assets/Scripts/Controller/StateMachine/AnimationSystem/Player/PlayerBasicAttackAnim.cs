using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Basic melee swing index (no skills): jatk01 / jatk02 / jatk03.
/// </summary>
public enum PlayerBasicAttackVariant
{
    Atk01 = 1,
    Atk02 = 2,
    Atk03 = 3
}

/// <summary>
/// Maps jatk01_/02_/03_ + weapon suffix → Animator state (CrossFade, not Trigger).
/// </summary>
public sealed class PlayerBasicAttackAnim : PlayerAnimStateMapBase
{
    public static readonly PlayerBasicAttackAnim Instance = new PlayerBasicAttackAnim();

    public const string Prefix01 = "jatk01_";
    public const string Prefix02 = "jatk02_";
    public const string Prefix03 = "jatk03_";

    static readonly string[] Prefixes =
    {
        Prefix01,
        Prefix02,
        Prefix03
    };

    protected override IReadOnlyList<string> PrefixesLongestFirst => Prefixes;

    public static string VariantPrefix(PlayerBasicAttackVariant variant)
    {
        switch (variant)
        {
            case PlayerBasicAttackVariant.Atk01: return Prefix01;
            case PlayerBasicAttackVariant.Atk02: return Prefix02;
            case PlayerBasicAttackVariant.Atk03: return Prefix03;
            default: return Prefix01;
        }
    }

    public static string ToStateName(PlayerBasicAttackVariant variant, PlayerAnimWeapon weapon) =>
        ComposeStateName(VariantPrefix(variant), weapon);

    public static bool TryParseVariantPrefix(string animName, out PlayerBasicAttackVariant variant, out string suffix)
    {
        variant = default;
        suffix = null;
        if (!Instance.TryResolveStateName(animName, out _, out string matchedPrefix))
        {
            return false;
        }

        if (!TryVariantFromPrefix(matchedPrefix, out variant))
        {
            return false;
        }

        suffix = animName.Substring(matchedPrefix.Length);
        return true;
    }

    /// <summary>
    /// Full name after GetFinalNameAnim, e.g. jatk01_1HS / jatk03_pole / jatk02_bow.
    /// </summary>
    public static bool TryResolve(string animName, out string stateName)
    {
        stateName = null;
        return Instance.TryResolveStateName(animName, out stateName, out _);
    }

    public static bool IsSwingClipName(string clipName)
    {
        if (string.IsNullOrEmpty(clipName))
            return false;
        if (clipName.IndexOf("wait", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;
        if (clipName.IndexOf("run", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;
        if (clipName.IndexOf("walk", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;
        return clipName.IndexOf("atk", StringComparison.OrdinalIgnoreCase) >= 0
            || clipName.IndexOf("jatk", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static bool TryGetSwingClipLength(
        Animator animator,
        int layer,
        out float length,
        out string clipName)
    {
        length = 0f;
        clipName = null;
        if (animator == null)
            return false;

        AnimatorClipInfo[] clips = null;
        if (animator.IsInTransition(layer))
            clips = animator.GetNextAnimatorClipInfo(layer);
        if (clips == null || clips.Length == 0 || clips[0].clip == null)
            clips = animator.GetCurrentAnimatorClipInfo(layer);
        if (clips == null || clips.Length == 0 || clips[0].clip == null)
            return false;
        if (!IsSwingClipName(clips[0].clip.name))
            return false;

        clipName = clips[0].clip.name;
        length = clips[0].clip.length;
        return length > 0.01f;
    }

    public static bool IsSwingPlaying(Animator animator, float minNormalized)
    {
        if (animator == null)
            return false;

        if (animator.IsInTransition(0))
        {
            AnimatorClipInfo[] nextClips = animator.GetNextAnimatorClipInfo(0);
            if (nextClips != null && nextClips.Length > 0 && nextClips[0].clip != null &&
                IsSwingClipName(nextClips[0].clip.name))
            {
                float nextN = animator.GetNextAnimatorStateInfo(0).normalizedTime;
                return nextN >= 0f && nextN < minNormalized;
            }
        }

        AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
        if (clips == null || clips.Length == 0 || clips[0].clip == null ||
            !IsSwingClipName(clips[0].clip.name))
            return false;

        float n = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        return n >= 0f && n < minNormalized;
    }

    static bool TryVariantFromPrefix(string prefix, out PlayerBasicAttackVariant variant)
    {
        switch (prefix)
        {
            case Prefix01:
                variant = PlayerBasicAttackVariant.Atk01;
                return true;
            case Prefix02:
                variant = PlayerBasicAttackVariant.Atk02;
                return true;
            case Prefix03:
                variant = PlayerBasicAttackVariant.Atk03;
                return true;
            default:
                variant = default;
                return false;
        }
    }
}
