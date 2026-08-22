using System;
using System.Collections.Generic;

/// <summary>
/// Special physical skill swing index: SpAtk01 / SpAtk02.
/// </summary>
public enum PlayerSpAtkVariant
{
    Sp01 = 1,
    Sp02 = 2
}

/// <summary>
/// Maps SpAtk01_/02_ + weapon suffix → Animator state (CrossFade, not Trigger).
/// </summary>
public sealed class PlayerSpAtkAnim : PlayerAnimStateMapBase
{
    public static readonly PlayerSpAtkAnim Instance = new PlayerSpAtkAnim();

    public const string Prefix01 = "SpAtk01_";
    public const string Prefix02 = "SpAtk02_";

    static readonly string[] Prefixes =
    {
        Prefix01,
        Prefix02
    };

    protected override IReadOnlyList<string> PrefixesLongestFirst => Prefixes;

    public static string VariantPrefix(PlayerSpAtkVariant variant)
    {
        switch (variant)
        {
            case PlayerSpAtkVariant.Sp01: return Prefix01;
            case PlayerSpAtkVariant.Sp02: return Prefix02;
            default: return Prefix01;
        }
    }

    public static string ToStateName(PlayerSpAtkVariant variant, PlayerAnimWeapon weapon) =>
        ComposeStateName(VariantPrefix(variant), weapon);

    /// <summary>
    /// Full name after GetFinalNameAnim, e.g. SpAtk01_1HS / SpAtk02_1HS / SpAtk01_bow.
    /// </summary>
    public static bool TryResolve(string animName, out string stateName)
    {
        stateName = null;
        return Instance.TryResolveStateName(animName, out stateName, out _);
    }
}
