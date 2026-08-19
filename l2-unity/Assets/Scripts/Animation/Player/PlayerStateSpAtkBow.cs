using UnityEngine;

/// <summary>
/// L2 bow skill (SpAtk01_bow): same model as <see cref="PlayerStateJatkBow"/>.
/// Shoot = OnAnimationShoot notify; arrow Accel via AbstractAttackEvents.CallBackStartShoot.
/// Requires Animator state Speed Parameter = patkspd.
/// Stretch uses skill HitTime (sptimeatk) when set, else CharInfo / UserInfo PAtkSpd.
/// </summary>
public class PlayerStateSpAtkBow : StateMachineBehaviour
{
    private float _startTime;
    private float _endTime;
    private float _linearSpeed;
    private bool _isSwitchIdle;

    public string parameterName;
    public string motionName;
    public string eventShootName;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _isSwitchIdle = false;
        _startTime = Time.time;

        float clipLength = AnimationDataCache.GetOverrideLength(animator, motionName);
        float eventTimeInClip = AnimationDataCache.GetEventTimeByName(animator, motionName, eventShootName);
        if (clipLength <= 0.01f)
            clipLength = Mathf.Max(stateInfo.length, 0.01f);
        if (eventTimeInClip <= 0.01f)
            eventTimeInClip = clipLength * 0.637f;

        int objectId = AnimatorUtils.GetObjectId(animator);
        int serverTimeMs = animator.GetInteger("sptimeatk");
        Entity entity = World.Instance != null ? World.Instance.GetEntityNoLockSync(objectId) : null;
        float cycleMs = serverTimeMs > 0
            ? serverTimeMs
            : AttackTimingHelper.ResolveAttackCycleMs(entity, parameterName);
        _endTime = Mathf.Max(0.01f, cycleMs / 1000f);
        _linearSpeed = AttackTimingHelper.ComputeLinearPAtkSpeed(clipLength, cycleMs);
        AnimationManager.Instance.SetPAtkSpeed(objectId, _linearSpeed);
        Debug.Log(
            $"[PATKSPD] SpAtkBow objectId={objectId} anim={parameterName} " +
            $"sptimeatk={serverTimeMs} cycleMs={cycleMs:F1} clipSec={clipLength:F3} patkspd={_linearSpeed:F3}");
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        int objectId = AnimatorUtils.GetObjectId(animator);
        AnimationManager.Instance.SetPAtkSpeed(objectId, _linearSpeed);

        if (!AnimatorUtils.IsLocalPlayerAnimator(animator))
            return;

        float timeOut = Time.time - _startTime;
        if (timeOut >= _endTime)
            SwitchToIdle(animator);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        AnimationManager.Instance.SetPAtkSpeed(AnimatorUtils.GetObjectId(animator), 1f);
    }

    private void SwitchToIdle(Animator animator)
    {
        if (_isSwitchIdle)
            return;

        _isSwitchIdle = true;
        if (PlayerEntity.Instance != null)
            PlayerEntity.Instance.IsAttack = false;

        int objectId = AnimatorUtils.GetObjectId(animator);
        string phaseName = AnimationManager.Instance != null
            ? AnimationManager.Instance.GetCurrentAnimationName(objectId)
            : null;
        if (string.IsNullOrEmpty(phaseName))
            phaseName = !string.IsNullOrEmpty(parameterName) ? parameterName : motionName;

        AnimationManager.Instance?.NotifyMagicPhaseFinished(objectId, phaseName);

        PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
        PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN, NewIdleState.WaitReturnFromCombatSmb);
    }
}
