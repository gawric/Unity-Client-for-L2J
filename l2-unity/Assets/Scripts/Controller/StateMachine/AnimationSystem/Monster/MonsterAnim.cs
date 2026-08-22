using System;

/// <summary>
/// Monster_Basic Animator states (exact names, no weapon suffix).
/// </summary>
public enum MonsterAnimState
{
    Wait,
    Walk,
    Run,
    SpAtk01,
    Atk01,
    Death,
    AtkWait
}

/// <summary>
/// Maps monster anim names → Animator state for CrossFadeInFixedTime (not bool graph).
/// </summary>
public static class MonsterAnim
{
    public const string Wait = "wait";
    public const string Walk = "walk";
    public const string Run = "run";
    public const string SpAtk01 = "spatk01";
    public const string Atk01 = "atk01";
    public const string Death = "death";
    public const string AtkWait = "atkwait";

    public static string ToStateName(MonsterAnimState state)
    {
        switch (state)
        {
            case MonsterAnimState.Wait: return Wait;
            case MonsterAnimState.Walk: return Walk;
            case MonsterAnimState.Run: return Run;
            case MonsterAnimState.SpAtk01: return SpAtk01;
            case MonsterAnimState.Atk01: return Atk01;
            case MonsterAnimState.Death: return Death;
            case MonsterAnimState.AtkWait: return AtkWait;
            default: return Wait;
        }
    }

    public static bool TryResolve(string animName, out string stateName, out MonsterAnimState family)
    {
        stateName = null;
        family = default;
        if (string.IsNullOrEmpty(animName))
        {
            return false;
        }

        // Exact match (AnimationNames.MONSTER_* already use bare names).
        if (string.Equals(animName, Wait, StringComparison.OrdinalIgnoreCase))
        {
            family = MonsterAnimState.Wait;
            stateName = Wait;
            return true;
        }

        if (string.Equals(animName, Walk, StringComparison.OrdinalIgnoreCase))
        {
            family = MonsterAnimState.Walk;
            stateName = Walk;
            return true;
        }

        if (string.Equals(animName, Run, StringComparison.OrdinalIgnoreCase))
        {
            family = MonsterAnimState.Run;
            stateName = Run;
            return true;
        }

        if (string.Equals(animName, SpAtk01, StringComparison.OrdinalIgnoreCase))
        {
            family = MonsterAnimState.SpAtk01;
            stateName = SpAtk01;
            return true;
        }

        if (string.Equals(animName, Atk01, StringComparison.OrdinalIgnoreCase))
        {
            family = MonsterAnimState.Atk01;
            stateName = Atk01;
            return true;
        }

        if (string.Equals(animName, Death, StringComparison.OrdinalIgnoreCase))
        {
            family = MonsterAnimState.Death;
            stateName = Death;
            return true;
        }

        if (string.Equals(animName, AtkWait, StringComparison.OrdinalIgnoreCase))
        {
            family = MonsterAnimState.AtkWait;
            stateName = AtkWait;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attack one-shots (atk / spatk) restart even if already in that state.
    /// Death is also a one-shot, but must not rewind: client predict + Die packet would restart the corpse.
    /// Looping locomotion may skip CrossFade when already playing.
    /// </summary>
    public static bool IsOneShot(MonsterAnimState family)
    {
        return family == MonsterAnimState.Atk01
            || family == MonsterAnimState.SpAtk01
            || family == MonsterAnimState.Death;
    }
}
