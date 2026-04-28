using UnityEngine;

public class PlayerStateJAtk : StateMachineBehaviour
{
    private const string JATK_TIMING_LOG = "[JATK_TIMING]";
    private float _startTime = -1;
    private float _endTime = -1;
    private float _remainingTime = 0f;
    private bool _isSwordHitLogged = false;
    private bool _isColliderSubscribed = false;

    public string parameterName;
    private AnimationCurve _animationCurve;
    private bool _isSwitchIdle = false;
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {

        AnimatorClipInfo[] clipInfos = animator.GetNextAnimatorClipInfo(layerIndex);

        if (clipInfos == null || clipInfos.Length == 0)
        {
            clipInfos = animator.GetCurrentAnimatorClipInfo(layerIndex);
        }

        _animationCurve = new AnimationCurve();
        _isSwitchIdle = false;
        _isSwordHitLogged = false;


        float timeAtk = GetTimeAtk(parameterName);


        _startTime = Time.time;
        _endTime = TimeUtils.ConvertMsToSec(timeAtk);
        _remainingTime = _endTime; // Initialize remaining time
        float timeAnimation = clipInfos[0].clip.length;
        //default
        //RecreateAnimationCurve(_animationCurve, _endTime, timeAnimation , 1.0f, 1.3f);
        RecreateAnimationCurve(_animationCurve, _endTime, timeAnimation, 1.0f, 1.1f);

        PlayerAnimationController.Instance.UpdateAnimatorAtkSpeedL2j(timeAtk, timeAnimation);
        TrySubscribeSwordCollider();

        float attackMs = TimeUtils.ConvertSecToMs(_endTime);
        float halfMs = attackMs * 0.5f;
        int entityId = PlayerEntity.Instance != null && PlayerEntity.Instance.IdentityInterlude != null ? PlayerEntity.Instance.IdentityInterlude.Id : 0;
        if (entityId > 0 && SwordCollisionService.Instance != null)
        {
            SwordCollisionService.Instance.UpdateAttackProgress(entityId, 0f);
        }
        Debug.Log($"{JATK_TIMING_LOG} Enter anim={parameterName} attackMs={attackMs:F1} halfMs={halfMs:F1} clipLenSec={timeAnimation:F3} player={PlayerEntity.Instance?.RandomName}");

        StopAnimationTrigger(animator, parameterName);
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float currentTime = Time.time;
        float timeOut = currentTime - _startTime;

        _remainingTime = Mathf.Max(0, _endTime - timeOut);
        float normalizedTime = timeOut / _endTime;
        float speed = _animationCurve.Evaluate(normalizedTime);
        if (timeOut >= _endTime) SwitchToIdle(stateInfo);
        int entityId = PlayerEntity.Instance != null && PlayerEntity.Instance.IdentityInterlude != null ? PlayerEntity.Instance.IdentityInterlude.Id : 0;
        if (entityId > 0 && SwordCollisionService.Instance != null)
        {
            SwordCollisionService.Instance.UpdateAttackProgress(entityId, TimeUtils.ConvertSecToMs(timeOut));
        }

        PlayerAnimationController.Instance.SetPAtkSpeed(speed);
    }

    private void SwitchToIdle(AnimatorStateInfo stateInfo)
    {

        float currentNormalizedTime = stateInfo.normalizedTime;

        if (!_isSwitchIdle & currentNormalizedTime > 0.9f & IsDieTarget() | !_isSwitchIdle & currentNormalizedTime > 0.9f & !PlayerEntity.Instance.IsAttack)
        {
            PlayerEntity.Instance.IsAttack = false;
            _isSwitchIdle = true;
            PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
            PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN);
        }
    }

    private bool IsDieTarget()
    {
        Entity entity = World.Instance.GetEntityNoLockSync(PlayerEntity.Instance.TargetId);
        if (entity != null) return entity.IsDead();
        return false;
    }
    private void StopAnimationTrigger(Animator animator, string parameterName)
    {
        if (animator.GetBool(parameterName) != false)
        {
            AnimationManager.Instance.StopCurrentAnimation(animator.GetInteger(AnimatorUtils.OBJECT_ID), parameterName, "player");
        }

    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        TryUnsubscribeSwordCollider();
        float elapsedMs = TimeUtils.ConvertSecToMs(Time.time - _startTime);
        int entityId = PlayerEntity.Instance != null && PlayerEntity.Instance.IdentityInterlude != null ? PlayerEntity.Instance.IdentityInterlude.Id : 0;
        if (entityId > 0 && SwordCollisionService.Instance != null)
        {
            SwordCollisionService.Instance.EndAttack(entityId);
        }
        Debug.Log($"{JATK_TIMING_LOG} Exit anim={parameterName} elapsedMs={elapsedMs:F1} expectedMs={TimeUtils.ConvertSecToMs(_endTime):F1} swordHit={_isSwordHitLogged}");
        PlayerEntity.Instance.IsAttack = false;
    }




    // Normal speed
    //RecreateAnimationCurve(myCurve, baseTimeAtk, baseTimeAnimation);

    // 2-5% faster attack (speedMultiplier = 0.95-0.98)
    //RecreateAnimationCurve(myCurve, baseTimeAtk, baseTimeAnimation, 0.96f, 1.0f);

    // Slower attack (if needed)
    //RecreateAnimationCurve(myCurve, baseTimeAtk, baseTimeAnimation, 1.0f, 1.2f);

    // Faster and slower combination
    // RecreateAnimationCurve(myCurve, baseTimeAtk, baseTimeAnimation, 0.97f, 1.1f);
    private void RecreateAnimationCurve(AnimationCurve animationCurve, float timeAtk, float timeAnimation,
    float speedMultiplier = 1.0f, float slowDownFactor = 1.0f)
    {

        float adjustedTimeAtk = timeAtk * speedMultiplier;

        float adjustedTimeAnimation = timeAnimation * slowDownFactor;

        animationCurve.keys = new Keyframe[0];

        Keyframe startKey = new Keyframe(0f, 0f);
        Keyframe windupKey = new Keyframe(adjustedTimeAtk * 0.2f, adjustedTimeAnimation * 0.15f); 
        Keyframe slowDownKey = new Keyframe(adjustedTimeAtk * 0.5f, adjustedTimeAnimation * 0.4f); 
        Keyframe powerKey = new Keyframe(adjustedTimeAtk * 0.8f, adjustedTimeAnimation * 0.8f);  
        Keyframe endKey = new Keyframe(adjustedTimeAtk, adjustedTimeAnimation); 


        animationCurve.AddKey(startKey);
        animationCurve.AddKey(windupKey);
        animationCurve.AddKey(slowDownKey);
        animationCurve.AddKey(powerKey);
        animationCurve.AddKey(endKey);


        animationCurve.preWrapMode = WrapMode.ClampForever;
        animationCurve.postWrapMode = WrapMode.ClampForever;

        for (int i = 1; i < animationCurve.keys.Length - 1; i++)
        {
            animationCurve.SmoothTangents(i, 0);
        }
    }




    private bool IsBow(string animName) => animName.IndexOf("bow") != -1;

    private void TrySubscribeSwordCollider()
    {
        if (_isColliderSubscribed || SwordCollisionService.Instance == null) return;
        SwordCollisionService.Instance.OnHitCollider += OnSwordColliderHit;
        _isColliderSubscribed = true;
    }

    private void TryUnsubscribeSwordCollider()
    {
        if (!_isColliderSubscribed || SwordCollisionService.Instance == null) return;
        SwordCollisionService.Instance.OnHitCollider -= OnSwordColliderHit;
        _isColliderSubscribed = false;
    }

    private void OnSwordColliderHit(Transform attacker, Transform target, Vector3 hitPointCollider, Vector3 hitDirection)
    {
        if (_startTime < 0f || _isSwordHitLogged || PlayerEntity.Instance == null) return;

        _isSwordHitLogged = true;
        float elapsedMs = TimeUtils.ConvertSecToMs(Time.time - _startTime);
        float expectedMs = TimeUtils.ConvertSecToMs(_endTime);
        float normalized = _endTime > 0f ? (Time.time - _startTime) / _endTime : 0f;
        bool sameRoot = false;
        if (attacker != null && PlayerEntity.Instance.transform != null)
        {
            sameRoot = attacker.root == PlayerEntity.Instance.transform.root;
        }

        string attackerName = attacker != null ? attacker.name : "null";
        string targetName = target != null ? target.name : "null";
        Debug.Log($"{JATK_TIMING_LOG} SwordHit anim={parameterName} elapsedMs={elapsedMs:F1} expectedMs={expectedMs:F1} normalized={normalized:F3} sameRoot={sameRoot} attacker={attackerName} target={targetName} hitPoint={hitPointCollider}");
    }

    private float GetTimeAtk(string animName)
    {
        if (IsBow(animName))
        {
            //return CalcBaseParam.CalculateTimeL2j(PlayerEntity.Instance.Stats.BasePAtkSpeed);
            float baseAttackTime = CalcBaseParam.CalculateTimeL2j(PlayerEntity.Instance.Stats.BasePAtkSpeed);
            float targetDistance = PlayerEntity.Instance.TargetDistance();
            float[] timeAndFlye = CalcBaseParam.CalculateAttackAndFlightTimes(targetDistance, baseAttackTime);


            return timeAndFlye[0];
            // return CalcBaseParam.CalculateTimeL2j(PlayerEntity.Instance.Stats.BasePAtkSpeed) / 2;
        }
        return CalcBaseParam.CalculateTimeL2j(PlayerEntity.Instance.Stats.BasePAtkSpeed) / 2;
    }




}