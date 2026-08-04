using System;
using UnityEngine;

public abstract class AnimationEventsBase : MonoBehaviour
{
    public event Action<string> OnAnimationFinished;
    public event Action<string> OnAnimationStartShoot;
    public event Action<string> OnAnimationStartHit;
    /// <summary>L2 AnimNotify_AttackShot — melee Hit/SoulShot only.</summary>
    public event Action<string> OnAnimationAttackShot;
    public event Action<string> OnAnimationFinishedHit;
    public event Action<string> OnAnimationStartLoadArrow;

    // Клипы иногда содержат несколько одинаковых Animation Events на одном кадре.
    private int _lastDuplicateCompleteFrame = int.MinValue;
    private string _lastDuplicateCompleteAnim;

    /// <summary>
    /// Unity Animation Event on clip end. Raw notify — no priority lock / queue.
    /// Phase await is owned by SMB (<see cref="AnimationManager.NotifyMagicPhaseFinished"/>);
    /// this remains a fallback for subscribers.
    /// </summary>
    public void OnAnimationComplete(string animationName)
    {
        animationName = NormalizeAnimationEventName(animationName);
        string finishedName = SkillAnimationDatabase.ResolveOverrideCompletionEventName(animationName);

        if (Time.frameCount == _lastDuplicateCompleteFrame &&
            string.Equals(_lastDuplicateCompleteAnim, finishedName, StringComparison.Ordinal))
        {
            return;
        }

        _lastDuplicateCompleteFrame = Time.frameCount;
        _lastDuplicateCompleteAnim = finishedName;

        OnAnimationFinished?.Invoke(finishedName);
    }

    /// <summary>
    /// Вызывается из Unity Animation Event на клипе. Базовая реализация только диспатчит подписчикам;
    /// у игрока/монстров переопределено в <see cref="BaseAnimationController"/> (лог + дедуп дублей).
    /// </summary>
    public virtual void OnAnimationShoot(string animationName)
    {
        animationName = NormalizeAnimationEventName(animationName);
        OnAnimationStartShoot?.Invoke(animationName);
    }

    private static string NormalizeAnimationEventName(string animationName)
    {
        if (string.IsNullOrEmpty(animationName))
        {
            return animationName;
        }

        // MagicNoTarget clips are played through the shared MagicShot animator state.
        // Some imported clips also carry the truncated event payload "MagicNoTarge".
        if (animationName == "MagicNoTarget" ||
            animationName == "MagicNoTarge" ||
            animationName == "MagicNotarget")
        {
            return "MagicShot";
        }

        return animationName;
    }

    /// <summary>
    /// Unity Animation Event on melee clips (L2 AnimNotify_AttackShot). Function name must be AttackShot.
    /// </summary>
    public void AttackShot(string animationName)
    {
        animationName = NormalizeAnimationEventName(animationName);
        int listeners = OnAnimationAttackShot != null ? OnAnimationAttackShot.GetInvocationList().Length : 0;
        Debug.Log(
            $"[HIT_FX] 1.AnimEvent AttackShot frame={Time.frameCount} t={Time.time:F3} " +
            $"anim={animationName} listeners={listeners}");
        OnAnimationAttackShot?.Invoke(animationName);
    }

    /// <summary>Legacy clip event — not used for melee Hit/SoulShot.</summary>
    public void OnAnimationHit(string animationName)
    {
        animationName = NormalizeAnimationEventName(animationName);
        OnAnimationStartHit?.Invoke(animationName);
    }

    public void OnAnimationAttackHitEnd(string animationName)
    {
        OnAnimationFinishedHit?.Invoke(animationName);
    }

    public void OnAnimationLoadArrow(string animationName)
    {
        OnAnimationStartLoadArrow?.Invoke(animationName);
    }
}
