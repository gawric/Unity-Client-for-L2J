using System;
using System.Collections.Generic;
using System.Linq;
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


    protected Dictionary<string, bool> _priorityAnimations = new Dictionary<string, bool>();
    protected Queue<string> _animationQueue = new Queue<string>();
    protected bool _isProcessingQueue = false;

    // Клипы иногда содержат несколько одинаковых Animation Events на одном времени —
    // на длинных MagicShot один кадр вызывал OnAnimationComplete 10+, ломая await/переходы.
    private int _lastDuplicateCompleteFrame = int.MinValue;
    private string _lastDuplicateCompleteAnim;

    /// <summary>
    /// Два CastEnd/CastMid подряд с разницей в несколько сотен ms (blend/два слоя/оверрайд) —
    /// второй всё равно вызывает OnAnimationFinished и HandleQueueAnimation → обрыв MagicShot.
    /// </summary>
    private readonly Dictionary<string, float> _lastPriorityCompleteRealtime = new Dictionary<string, float>();
    private const float PriorityCompleteDebounceSeconds = 0.35f;

    /// <summary>
    /// Same-name re-attack (jatk03→jatk03): animator fires OnAnimationComplete for the OLD clip
    /// while the priority flag is still true → CLEAR/IDLE kills the new swing (no AttackShot).
    /// Relock sets a short suppress window so that exit-complete is ignored.
    /// </summary>
    private readonly Dictionary<string, float> _suppressCompleteUntilRealtime = new Dictionary<string, float>();
    private const float SameNameRelockSuppressSeconds = 0.40f;


    public void InitializePriority()
    {
        _priorityAnimations = new Dictionary<string, bool>
        {
            //magic atk
            { "MagicShot", false },
            { "MagicShot2P", false },
            { "CastMid", false },
            { "CastEnd", false },
            { "CastEnd2P", false },
            { "CastMidLong", false },
            { "CastEndLong", false },
            { "MagicShotLong", false },
             //magic end
            { "jatk01_bow", false },
            { "jatk02_bow", false },
            { "jatk03_bow", false },

            { "jatk01_dual", false },
            { "jatk02_dual", false },
            { "jatk03_dual", false },

            { "jatk01_2HS", false },
            { "jatk02_2HS", false },
            { "jatk03_2HS", false },

            { "jatk01_1HS", false },
            { "jatk02_1HS", false },
            { "jatk03_1HS", false },

            { "jatk01_pole", false },
            { "jatk02_pole", false },
            { "jatk03_pole", false },

            { "SpAtk01_1HS", false },
            { "SpAtk02_1HS", false },
            { "SpAtk01_2HS", false },
            { "SpAtk01_pole", false },
            { "SpAtk01_bow", false },
        };
    }
    public void OnAnimationComplete(string animationName)
    {
        animationName = NormalizeAnimationEventName(animationName);

        // Clip events use base names (CastEnd / MagicShot). 2P/Long triggers lock CastEnd2P etc.
        // Without remap, Complete=CastEnd + lock=CastEnd2P → STALE_IGNORE → runner never advances.
        string eventName = animationName;
        string lockKey = ResolvePriorityLockKeyForComplete(eventName);

        if (!_priorityAnimations.ContainsKey(lockKey) && !_priorityAnimations.ContainsKey(eventName))
        {
            Debug.Log(
                $"[ANIM_PRIORITY_Q] OnAnimationComplete IGNORED anim={eventName} " +
                $"(not in priority dict — will NOT unlock queue)");
            return;
        }

        if (!_priorityAnimations.ContainsKey(lockKey))
        {
            lockKey = eventName;
        }

        if (Time.frameCount == _lastDuplicateCompleteFrame &&
            string.Equals(_lastDuplicateCompleteAnim, lockKey, StringComparison.Ordinal))
        {
            Debug.Log(
                $"[ANIM_PRIORITY_Q] OnAnimationComplete DEDUP_FRAME anim={eventName} lock={lockKey} " +
                $"(same frame duplicate — flag NOT cleared)");
            return;
        }

        float nowRt = Time.time;
        if (_lastPriorityCompleteRealtime.TryGetValue(lockKey, out float prevRt) &&
            nowRt - prevRt < PriorityCompleteDebounceSeconds)
        {
            Debug.Log(
                $"[ANIM_PRIORITY_Q] OnAnimationComplete DEDUP_DEBOUNCE anim={eventName} lock={lockKey} " +
                $"dt={nowRt - prevRt:F3}s < {PriorityCompleteDebounceSeconds}s " +
                $"(flag NOT cleared — queue stays locked!)");
            return;
        }

        bool wasTrue = _priorityAnimations[lockKey];
        var activeNow = _priorityAnimations.Where(kv => kv.Value).Select(kv => kv.Key).ToArray();

        if (!string.Equals(lockKey, eventName, StringComparison.Ordinal) && wasTrue)
        {
            Debug.Log(
                $"[ANIM_PRIORITY_Q] OnAnimationComplete REMAP event={eventName} → lock={lockKey} " +
                $"activePriority=[{string.Join(",", activeNow)}]");
        }

        // Interrupted swing: flag already cleared when a newer jatk locked.
        // Must NOT Idle/WAIT_RETURN — that FORCE-unlocks the live attack.
        if (!wasTrue)
        {
            Debug.Log(
                $"[ANIM_PRIORITY_Q] OnAnimationComplete STALE_IGNORE anim={eventName} lock={lockKey} " +
                $"activePriority=[{string.Join(",", activeNow)}] " +
                $"(not live lock — skip Finished/Idle so next swing survives)");
            return;
        }

        // Same-name re-lock: ignore Completes from the previous clip instance.
        if (_suppressCompleteUntilRealtime.TryGetValue(lockKey, out float suppressUntil) &&
            nowRt < suppressUntil)
        {
            Debug.Log(
                $"[ANIM_PRIORITY_Q] OnAnimationComplete STALE_IGNORE anim={eventName} lock={lockKey} " +
                $"activePriority=[{string.Join(",", activeNow)}] " +
                $"remainSuppress={suppressUntil - nowRt:F3}s " +
                $"(same-name relock — skip Finished/Idle so new swing survives)");
            return;
        }

        // Only the live priority anim may finish the queue / notify subscribers.
        if (activeNow.Length > 0 &&
            !activeNow.Any(a => string.Equals(a, lockKey, StringComparison.Ordinal)))
        {
            Debug.Log(
                $"[ANIM_PRIORITY_Q] OnAnimationComplete STALE_IGNORE anim={eventName} lock={lockKey} " +
                $"activePriority=[{string.Join(",", activeNow)}] " +
                $"(complete is not the active lock)");
            return;
        }

        _lastDuplicateCompleteFrame = Time.frameCount;
        _lastDuplicateCompleteAnim = lockKey;
        _lastPriorityCompleteRealtime[lockKey] = nowRt;

        _priorityAnimations[lockKey] = false;
        _isProcessingQueue = false;

        // Await expects clip event name (CastEnd), not trigger key (CastEnd2P).
        string finishedName = SkillAnimationDatabase.ResolveOverrideCompletionEventName(lockKey);

        var activeLeft = _priorityAnimations.Where(kv => kv.Value).Select(kv => kv.Key).ToArray();
        Debug.Log(
            $"[ANIM_PRIORITY_Q] OnAnimationComplete CLEAR anim={eventName} lock={lockKey} " +
            $"finishedName={finishedName} wasTrue={wasTrue} " +
            $"isProcessingQueue=False queueCount={_animationQueue.Count} " +
            $"stillActive=[{string.Join(",", activeLeft)}] " +
            $"queue=[{string.Join(",", _animationQueue)}]");

        OnAnimationFinished?.Invoke(finishedName);

        if (_animationQueue.Count > 0)
        {
            var lastAnimation = _animationQueue.Last();

            foreach (string animName in _animationQueue)
            {
                Debug.Log($"AnimationManager> start name убираем в листе ожиданий iteration animName " + animName + " _animationQueue  " + _animationQueue.Count);
            }

            Debug.Log($"[ANIM_PRIORITY_Q] Drain queue → play last={lastAnimation}");
            HandleQueueAnimation(lastAnimation);
            _animationQueue.Clear();
        }
    }

    /// <summary>
    /// Maps Complete event (CastEnd) to the live priority lock (CastEnd2P / CastEndLong / …).
    /// </summary>
    private string ResolvePriorityLockKeyForComplete(string completeEventName)
    {
        if (string.IsNullOrEmpty(completeEventName))
        {
            return completeEventName;
        }

        if (_priorityAnimations.TryGetValue(completeEventName, out bool directLive) && directLive)
        {
            return completeEventName;
        }

        foreach (var kv in _priorityAnimations)
        {
            if (!kv.Value)
            {
                continue;
            }

            string completionForLock = SkillAnimationDatabase.ResolveOverrideCompletionEventName(kv.Key);
            if (string.Equals(completionForLock, completeEventName, StringComparison.Ordinal))
            {
                return kv.Key;
            }
        }

        return completeEventName;
    }


    protected virtual void HandleQueueAnimation(string animationName)
    {
        // Base implementation does nothing, derived class will implement it
    }

    /// <summary>
    /// Called when a priority anim is re-triggered while its lock flag is already true
    /// (e.g. server sends next Attack with the same random jatk03).
    /// </summary>
    protected void MarkSameNamePriorityRelock(string animName)
    {
        if (string.IsNullOrEmpty(animName)) return;
        float until = Time.time + SameNameRelockSuppressSeconds;
        _suppressCompleteUntilRealtime[animName] = until;
        Debug.Log(
            $"[ANIM_PRIORITY_Q] RELOCK same-name anim={animName} " +
            $"suppressCompleteFor={SameNameRelockSuppressSeconds:F2}s until t={until:F3}");
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