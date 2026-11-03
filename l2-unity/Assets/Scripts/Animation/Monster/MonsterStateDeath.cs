using UnityEngine;

public class MonsterStateDeath : MonsterStateBase
{
    public string parameterName;
    bool _sawFall;
    int _rewinds;
    bool _pinning;
    bool _loggedSettle;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        LoadComponents(animator);
        StopAnimationTrigger(animator, parameterName);

        bool fresh = IsFreshFall(animator, stateInfo, layerIndex);
        float speedBefore = animator != null ? animator.speed : -1f;
        // CrossFade back into this same state is the rewind. A fall that starts
        // from run/atk still reports normalizedTime 0 and must clear a stale hold.
        if (fresh)
        {
            _sawFall = false;
            _rewinds = 0;
            _pinning = false;
            _loggedSettle = false;
            if (_entity != null && _entity.ActionSlot != null)
                _entity.ActionSlot.DeathPoseHeld = false;
        }

        if (fresh)
            SetSpeed(animator, 1f);
        else if (IsPoseHeld() || _pinning)
            SetSpeed(animator, 0f);
        else
            SetSpeed(animator, 1f);
        Debug.Log(
            "[DEATH_SPEED] Enter id=" + ObjectId(animator) +
            " name=" + EntityName() +
            " fresh=" + fresh +
            " held=" + IsPoseHeld() +
            " speedBefore=" + speedBefore.ToString("F3") +
            " " + Pose(animator, stateInfo, layerIndex));
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator == null)
            return;

        // Speed 0 before the fall freezes the blend on the standing pose.
        // After the fall, speed 0 holds the corpse. Play() would re-enter this state.
        float speedBefore = animator.speed;
        if (!_loggedSettle && !animator.IsInTransition(layerIndex) &&
            (speedBefore < 0.05f || stateInfo.normalizedTime >= 1f))
        {
            _loggedSettle = true;
            Debug.Log(
                "[DEATH_SPEED] Settle id=" + ObjectId(animator) +
                " name=" + EntityName() +
                " sawFall=" + _sawFall +
                " rewinds=" + _rewinds +
                " speedBefore=" + speedBefore.ToString("F3") +
                " " + Pose(animator, stateInfo, layerIndex));
        }

        if (_pinning)
        {
            SetSpeed(animator, 0f);
            return;
        }

        SetSpeed(animator, 1f);

        if (animator.IsInTransition(layerIndex))
            return;

        if (stateInfo.normalizedTime < 0.5f)
            _sawFall = true;

        if (stateInfo.normalizedTime < 1f)
            return;

        // Enter reported 0 during the blend, then the first settled sample was already
        // past the end. Rewind once so the fall is visible.
        if (!_sawFall && _rewinds < 1)
        {
            _rewinds++;
            Debug.Log(
                "[DEATH_SPEED] Rewind id=" + ObjectId(animator) +
                " name=" + EntityName() +
                " " + Pose(animator, stateInfo, layerIndex));
            animator.CrossFadeInFixedTime(stateInfo.fullPathHash, 0.05f, layerIndex, 0f);
            return;
        }

        _pinning = true;
        if (_entity != null && _entity.ActionSlot != null)
            _entity.ActionSlot.DeathPoseHeld = true;
        SetSpeed(animator, 0f);
        Debug.Log(
            "[DEATH_SPEED] Pin id=" + ObjectId(animator) +
            " name=" + EntityName() +
            " sawFall=" + _sawFall +
            " " + Pose(animator, stateInfo, layerIndex));
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_pinning || _rewinds > 0)
            return;

        bool backToDeath = animator != null
            && animator.IsInTransition(layerIndex)
            && animator.GetNextAnimatorStateInfo(layerIndex).IsName(MonsterAnim.Death);
        if (backToDeath)
            return;

        LoadComponents(animator);
        SetSpeed(animator, 1f);
        if (_entity != null && _entity.ActionSlot != null)
            _entity.ActionSlot.DeathPoseHeld = false;
        Debug.Log(
            "[DEATH_SPEED] Exit id=" + ObjectId(animator) +
            " name=" + EntityName() +
            " " + Pose(animator, stateInfo, layerIndex));
    }

    bool IsPoseHeld()
    {
        return _entity != null && _entity.ActionSlot != null && _entity.ActionSlot.DeathPoseHeld;
    }

    string Pose(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        string current = "-";
        string next = "-";
        if (animator != null)
        {
            AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(layerIndex);
            if (clips != null && clips.Length > 0 && clips[0].clip != null)
                current = clips[0].clip.name;
            if (animator.IsInTransition(layerIndex))
            {
                AnimatorClipInfo[] nextClips = animator.GetNextAnimatorClipInfo(layerIndex);
                if (nextClips != null && nextClips.Length > 0 && nextClips[0].clip != null)
                    next = nextClips[0].clip.name;
            }
        }

        return "speed=" + (animator != null ? animator.speed.ToString("F3") : "-") +
            " norm=" + stateInfo.normalizedTime.ToString("F3") +
            " len=" + stateInfo.length.ToString("F3") +
            " transition=" + (animator != null && animator.IsInTransition(layerIndex)) +
            " clip=" + current +
            " next=" + next;
    }

    int ObjectId(Animator animator)
    {
        if (_entity != null && _entity.Identity != null)
            return _entity.Identity.Id;
        if (animator == null)
            return 0;
        return animator.GetInteger(AnimatorUtils.OBJECT_ID);
    }

    string EntityName()
    {
        if (_entity == null)
            return "-";
        if (_entity.Identity != null && !string.IsNullOrEmpty(_entity.Identity.Name))
            return _entity.Identity.Name;
        return _entity.name;
    }

    bool IsFreshFall(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator == null || stateInfo.normalizedTime >= 0.05f)
            return false;
        if (!animator.IsInTransition(layerIndex))
            return !animator.GetCurrentAnimatorStateInfo(layerIndex).IsName(MonsterAnim.Death);

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layerIndex);
        if (current.IsName(MonsterAnim.Death))
            return false;

        AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(layerIndex);
        if (clips != null && clips.Length > 0 && clips[0].clip != null &&
            clips[0].clip.name.IndexOf("death", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return false;

        return true;
    }

    void SetSpeed(Animator animator, float speed)
    {
        if (_networkAnimationController != null)
            _networkAnimationController.SetAnimatorSpeed(speed);
        else if (animator != null)
            animator.speed = speed;
    }

    private void StopAnimationTrigger(Animator animator, string parameterName)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName))
            return;
        if (animator.GetBool(parameterName) != false)
        {
            AnimationManager.Instance.StopMonsterCurrentAnimation(animator.GetInteger(AnimatorUtils.OBJECT_ID), parameterName);
        }
    }
}
