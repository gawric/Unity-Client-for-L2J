using UnityEngine;

public class PlayerStateSpAtk : StateMachineBehaviour
{
    public const float HitTimeCompensationSeconds = 0.08f;
    public const float MinServerHitSeconds = 0.1f;
    public const float SwitchToIdleNormalizedTime = 0.95f;

    private float _startTime;
    private float _eventHitTimeInClip;
    private float _serverHitTime;
    private bool _isSwitchIdle;
    private bool _hitExecuted;

    public string motionName;
    public string eventStartHitName; // "hit_start"
    public string eventEndHitName;   // "hit_end"

    [Header("Legacy (unused in linear mode)")]
    public float postHitSpeed = 2.0f;

    /// <summary>
    /// Wall-clock duration until SwitchToIdle (norm >= 0.95).
    /// Linear mode: one speed for the whole clip (hit synced to server HitTime).
    /// </summary>
    public static bool TryEstimateWallDuration(
        Animator animator,
        string spatkAnimName,
        int serverHitTimeMs,
        out float wallDurationSeconds,
        out string debugMotionName)
    {
        wallDurationSeconds = -1f;
        debugMotionName = null;
        if (animator == null || serverHitTimeMs <= 0)
        {
            return false;
        }

        if (!TryResolveBehaviour(animator, spatkAnimName, out PlayerStateSpAtk behaviour) || behaviour == null)
        {
            return false;
        }

        debugMotionName = behaviour.motionName;
        float clipLength = AnimationDataCache.GetOverrideLength(animator, behaviour.motionName);
        float startHit = AnimationDataCache.GetEventTimeByName(animator, behaviour.motionName, behaviour.eventStartHitName);
        float endHit = AnimationDataCache.GetEventTimeByName(animator, behaviour.motionName, behaviour.eventEndHitName);
        float hitWindowCenter = (startHit + endHit) * 0.5f;
        if (hitWindowCenter <= 0.01f)
        {
            return false;
        }

        float serverHitTime = (serverHitTimeMs / 1000f) - HitTimeCompensationSeconds;
        if (serverHitTime < MinServerHitSeconds)
        {
            serverHitTime = MinServerHitSeconds;
        }

        float linearSpeed = hitWindowCenter / serverHitTime;
        wallDurationSeconds = (SwitchToIdleNormalizedTime * clipLength) / Mathf.Max(0.01f, linearSpeed);
        return wallDurationSeconds > 0f;
    }

    private static bool TryResolveBehaviour(Animator animator, string spatkAnimName, out PlayerStateSpAtk behaviour)
    {
        behaviour = null;
        PlayerStateSpAtk[] behaviours = animator.GetBehaviours<PlayerStateSpAtk>();
        if (behaviours == null || behaviours.Length == 0)
        {
            return false;
        }

        if (behaviours.Length == 1)
        {
            behaviour = behaviours[0];
            return true;
        }

        bool want02 = !string.IsNullOrEmpty(spatkAnimName) &&
                      spatkAnimName.IndexOf("SpAtk02", System.StringComparison.OrdinalIgnoreCase) >= 0;
        for (int i = 0; i < behaviours.Length; i++)
        {
            PlayerStateSpAtk candidate = behaviours[i];
            if (candidate == null || string.IsNullOrEmpty(candidate.motionName))
            {
                continue;
            }

            string motion = candidate.motionName;
            bool is02 = motion.IndexOf("spatk002", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        motion.IndexOf("spatk02", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        motion.IndexOf("SpAtk02", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (is02 == want02)
            {
                behaviour = candidate;
                return true;
            }
        }

        behaviour = behaviours[0];
        return behaviour != null;
    }

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _isSwitchIdle = false;
        _hitExecuted = false;
        _startTime = Time.time;

        float startHit = AnimationDataCache.GetEventTimeByName(animator, motionName, eventStartHitName);
        float endHit = AnimationDataCache.GetEventTimeByName(animator, motionName, eventEndHitName);
        _eventHitTimeInClip = (startHit + endHit) / 2f;

        int serverTimeMs = animator.GetInteger("sptimeatk");
        _serverHitTime = (serverTimeMs / 1000f) - HitTimeCompensationSeconds;
        if (_serverHitTime < MinServerHitSeconds)
        {
            _serverHitTime = MinServerHitSeconds;
        }

        // One speed for the whole clip; hit lands at server HitTime.
        animator.speed = _eventHitTimeInClip / _serverHitTime;
        animator.Update(0);
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float elapsed = Time.time - _startTime;

        if (elapsed >= _serverHitTime && !_hitExecuted)
        {
            _hitExecuted = true;
        }

        if (stateInfo.normalizedTime >= SwitchToIdleNormalizedTime)
        {
            SwitchToIdle(animator);
        }
    }

    private void SwitchToIdle(Animator animator)
    {
        if (_isSwitchIdle)
        {
            return;
        }

        _isSwitchIdle = true;
        animator.speed = 1f;
        PlayerEntity.Instance.IsAttack = false;

        int objectId = animator.GetInteger(AnimatorUtils.OBJECT_ID);
        string phaseName = AnimationManager.Instance != null
            ? AnimationManager.Instance.GetCurrentAnimationName(objectId)
            : null;
        if (string.IsNullOrEmpty(phaseName))
        {
            phaseName = motionName;
        }

        AnimationManager.Instance?.NotifyMagicPhaseFinished(objectId, phaseName);

        PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
        PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN, NewIdleState.WaitReturnFromCombatSmb);
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.speed = 1f;
    }
}
