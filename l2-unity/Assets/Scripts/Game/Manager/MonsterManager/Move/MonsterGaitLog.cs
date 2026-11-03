using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Monster slides while the pawn keeps moving. Logs the animation request and the
/// frame where the body is standing or the clip is not advancing.
/// </summary>
public static class MonsterGaitLog
{
    const float AskRepeatSec = 0.75f;
    const float StallRepeatSec = 1f;
    const float FrozenNormSec = 0.35f;

    static readonly Dictionary<int, float> _askAt = new Dictionary<int, float>(64);
    static readonly Dictionary<int, string> _askWhere = new Dictionary<int, string>(64);
    static readonly Dictionary<int, string> _lastAsk = new Dictionary<int, string>(64);
    static readonly Dictionary<int, float> _stallAt = new Dictionary<int, float>(64);
    static readonly Dictionary<int, bool> _stalling = new Dictionary<int, bool>(64);
    static readonly Dictionary<int, float> _prevNorm = new Dictionary<int, float>(64);
    static readonly Dictionary<int, float> _normMovedAt = new Dictionary<int, float>(64);
    static readonly Dictionary<int, float> _speedZeroAt = new Dictionary<int, float>(32);

    public static void Ask(Entity entity, string where, string detail)
    {
        if (!(entity is MonsterEntity))
            return;

        int id = Id(entity);
        float now = Time.time;
        string key = where + "|" + (detail ?? "");
        string prevWhere;
        _askWhere.TryGetValue(id, out prevWhere);
        float prevAt;
        _askAt.TryGetValue(id, out prevAt);
        if (prevWhere == key && now - prevAt < AskRepeatSec)
            return;

        _askWhere[id] = key;
        _askAt[id] = now;
        string line = where + (string.IsNullOrEmpty(detail) ? "" : " " + detail);
        _lastAsk[id] = line;
        Debug.Log(
            "[MONSTER_GAIT] ask id=" + id +
            " name=" + Name(entity) +
            " " + line +
            " " + Pose(entity));
    }

    public static void Play(int objectId, string state, bool already, bool played)
    {
        if (state != MonsterAnim.Wait && state != MonsterAnim.Walk &&
            state != MonsterAnim.Run && state != MonsterAnim.AtkWait)
            return;

        Entity entity = Find(objectId);
        if (!(entity is MonsterEntity))
            return;

        Ask(entity, "play",
            "state=" + state +
            " already=" + already +
            " played=" + played +
            " durationSkip=" + (!played && already));
    }

    public static void SpeedZero(Animator animator, float speed)
    {
        if (animator == null || speed >= 0.05f)
            return;

        int id = animator.GetInteger(AnimatorUtils.OBJECT_ID);
        Entity entity = Find(id);
        if (!(entity is MonsterEntity) || entity.IsDead())
            return;

        float now = Time.time;
        float prev;
        if (_speedZeroAt.TryGetValue(id, out prev) && now - prev < StallRepeatSec)
            return;
        _speedZeroAt[id] = now;
        _lastAsk[id] = "speed-zero";
        Debug.Log(
            "[MONSTER_GAIT] speed-zero id=" + id +
            " name=" + Name(entity) +
            " set=" + speed.ToString("F3") +
            " " + Pose(entity));
    }

    public static void SampleMove(Entity entity, float moved, string cachedGait)
    {
        if (!(entity is MonsterEntity) || entity.IsDead() || moved < 0.002f)
            return;

        PlayerEntity player = PlayerEntity.Instance;
        if (player != null &&
            VectorUtils.Distance2D(entity.transform.position, player.transform.position) > 30f)
            return;

        int id = Id(entity);
        Animator animator = AnimatorOf(entity);
        string why = StallReason(id, animator);
        if (why == null)
        {
            if (_stalling.TryGetValue(id, out bool was) && was)
            {
                _stalling[id] = false;
                Debug.Log(
                    "[MONSTER_GAIT] recover id=" + id +
                    " name=" + Name(entity) +
                    " moved=" + moved.ToString("F3") +
                    " " + Pose(entity));
            }
            return;
        }

        float now = Time.time;
        bool first = !_stalling.TryGetValue(id, out bool stalling) || !stalling;
        float prev;
        if (!first && _stallAt.TryGetValue(id, out prev) && now - prev < StallRepeatSec)
            return;

        _stalling[id] = true;
        _stallAt[id] = now;
        string last;
        if (!_lastAsk.TryGetValue(id, out last))
            last = "-";
        Debug.Log(
            "[MONSTER_GAIT] stall id=" + id +
            " name=" + Name(entity) +
            " why=" + why +
            " moved=" + moved.ToString("F3") +
            " cached=" + (string.IsNullOrEmpty(cachedGait) ? "none" : cachedGait) +
            " running=" + entity.Running +
            " lastAsk=" + last +
            " " + Pose(entity));
    }

    static string StallReason(int id, Animator animator)
    {
        if (animator == null)
            return "no-animator";
        if (!animator.enabled)
            return "animator-disabled";
        if (animator.speed < 0.05f)
            return "speed-zero";

        bool intoMove = IsMoveState(animator, 0);
        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
            if (next.IsName(MonsterAnim.Walk) || next.IsName(MonsterAnim.Run))
                intoMove = true;
        }

        if (!intoMove)
        {
            if (IsAttackState(animator))
                return null;
            return "standing";
        }

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        float now = Time.time;
        float prevNorm;
        bool had = _prevNorm.TryGetValue(id, out prevNorm);
        _prevNorm[id] = state.normalizedTime;
        if (!had || Mathf.Abs(state.normalizedTime - prevNorm) > 0.0008f)
        {
            _normMovedAt[id] = now;
            return null;
        }

        float since;
        if (!_normMovedAt.TryGetValue(id, out since))
            since = now;
        if (now - since < FrozenNormSec)
            return null;
        return "clip-frozen";
    }

    static bool IsMoveState(Animator animator, int layer)
    {
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(layer);
        return state.IsName(MonsterAnim.Walk) || state.IsName(MonsterAnim.Run);
    }

    static bool IsAttackState(Animator animator)
    {
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName(MonsterAnim.Atk01) || state.IsName(MonsterAnim.SpAtk01))
            return state.normalizedTime < 1.05f;
        if (!animator.IsInTransition(0))
            return false;
        AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
        return next.IsName(MonsterAnim.Atk01) || next.IsName(MonsterAnim.SpAtk01);
    }

    static string Pose(Entity entity)
    {
        Animator animator = AnimatorOf(entity);
        string clip = "-";
        string next = "-";
        string speed = "-";
        string norm = "-";
        string transition = "-";
        string enabled = "-";
        string cull = "-";
        if (animator != null)
        {
            speed = animator.speed.ToString("F3");
            enabled = animator.enabled.ToString();
            cull = animator.cullingMode.ToString();
            transition = animator.IsInTransition(0).ToString();
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            norm = state.normalizedTime.ToString("F3");
            AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
            if (clips != null && clips.Length > 0 && clips[0].clip != null)
                clip = clips[0].clip.name;
            if (animator.IsInTransition(0))
            {
                AnimatorClipInfo[] nextClips = animator.GetNextAnimatorClipInfo(0);
                if (nextClips != null && nextClips.Length > 0 && nextClips[0].clip != null)
                    next = nextClips[0].clip.name;
            }
        }

        string action = entity != null && entity.ActionSlot != null
            ? entity.ActionSlot.Action.ToString()
            : "-";
        bool held = entity != null && entity.ActionSlot != null && entity.ActionSlot.DeathPoseHeld;
        return "action=" + action +
            " dead=" + (entity != null && entity.IsDead()) +
            " held=" + held +
            " speed=" + speed +
            " norm=" + norm +
            " transition=" + transition +
            " enabled=" + enabled +
            " cull=" + cull +
            " clip=" + clip +
            " next=" + next;
    }

    static Animator AnimatorOf(Entity entity)
    {
        if (entity == null)
            return null;
        NetworkAnimationController controller = entity.GetAnimatorController();
        return controller != null ? controller.GetAnimator() : null;
    }

    static Entity Find(int objectId)
    {
        if (objectId == 0 || IncomingPacketActions.GameWorld == null)
            return null;
        return IncomingPacketActions.GameWorld.GetEntityNoLockSync(objectId);
    }

    static int Id(Entity entity)
    {
        return entity != null && entity.Identity != null ? entity.Identity.Id : 0;
    }

    static string Name(Entity entity)
    {
        if (entity == null)
            return "-";
        if (entity.Identity != null && !string.IsNullOrEmpty(entity.Identity.Name))
            return entity.Identity.Name;
        return entity.name;
    }
}
