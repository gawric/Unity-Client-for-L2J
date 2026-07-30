using System;
using System.Collections.Generic;

/// <summary>
/// Weapon suffix shared by player Animator states
/// (wait/walk/run/atkwait/jatk… + _hand / _1HS / _2HS / _pole / _dual / _bow).
/// </summary>
public enum PlayerAnimWeapon
{
    Hand,
    OneHS,
    TwoHS,
    Pole,
    Dual,
    Bow
}

/// <summary>
/// Base map for player Animator state names: family/variant prefix + weapon suffix.
/// Used by locomotion (wait/walk/run/atkwait) and basic melee (jatk01/02/03).
/// Concrete maps supply ordered prefixes (longest first when they overlap).
/// </summary>
public abstract class PlayerAnimStateMapBase
{
    /// <summary>Prefixes checked in order; put longer ones first (e.g. atkwait_ before wait_).</summary>
    protected abstract IReadOnlyList<string> PrefixesLongestFirst { get; }

    public static string ToWeaponSuffix(PlayerAnimWeapon weapon)
    {
        switch (weapon)
        {
            case PlayerAnimWeapon.Hand: return "hand";
            case PlayerAnimWeapon.OneHS: return "1HS";
            case PlayerAnimWeapon.TwoHS: return "2HS";
            case PlayerAnimWeapon.Pole: return "pole";
            case PlayerAnimWeapon.Dual: return "dual";
            case PlayerAnimWeapon.Bow: return "bow";
            default: return "hand";
        }
    }

    public static bool TryParseWeaponSuffix(string weaponSuffix, out PlayerAnimWeapon weapon)
    {
        weapon = PlayerAnimWeapon.Hand;
        if (string.IsNullOrEmpty(weaponSuffix))
        {
            return false;
        }

        switch (weaponSuffix)
        {
            case "hand":
                weapon = PlayerAnimWeapon.Hand;
                return true;
            case "1HS":
                weapon = PlayerAnimWeapon.OneHS;
                return true;
            case "2HS":
                weapon = PlayerAnimWeapon.TwoHS;
                return true;
            case "pole":
                weapon = PlayerAnimWeapon.Pole;
                return true;
            case "dual":
                weapon = PlayerAnimWeapon.Dual;
                return true;
            case "bow":
                weapon = PlayerAnimWeapon.Bow;
                return true;
            default:
                return false;
        }
    }

    public static string ComposeStateName(string prefix, PlayerAnimWeapon weapon) =>
        prefix + ToWeaponSuffix(weapon);

    /// <summary>
    /// Split animName into known prefix + weapon suffix, then rebuild canonical state name.
    /// </summary>
    protected bool TryResolveStateName(string animName, out string stateName, out string matchedPrefix)
    {
        stateName = null;
        matchedPrefix = null;
        if (string.IsNullOrEmpty(animName))
        {
            return false;
        }

        IReadOnlyList<string> prefixes = PrefixesLongestFirst;
        for (int i = 0; i < prefixes.Count; i++)
        {
            string prefix = prefixes[i];
            if (!animName.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            string suffix = animName.Substring(prefix.Length);
            if (!TryParseWeaponSuffix(suffix, out PlayerAnimWeapon weapon))
            {
                return false;
            }

            matchedPrefix = prefix;
            stateName = ComposeStateName(prefix, weapon);
            return true;
        }

        return false;
    }
}
