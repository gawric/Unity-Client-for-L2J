using System;
using System.Collections.Generic;

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
