using System;
using System.Collections.Generic;
using UnityEngine;

public class SwordCollisionService : MonoBehaviour
{
    private const string TIMER_LOG = "[SWORD_TIMER]";
    private const string CHAIN_LOG = "[ATK_HIT_CHAIN]";
    private const float DEFAULT_HIT_FRACTION = 0.88f;
    private const float DEFAULT_ATTACK_DURATION_MS = 1000f;

    private sealed class AttackTimingContext
    {
        public int EntityId;
        public int TargetEntityId;
        public Transform AttackerTransform;
        public Transform TargetTransform;
        public float StartTimeSec;
        public float DurationMs;
        public float HitFraction;
        public float LastElapsedMs;
        public int Epoch;
        public bool AttackShotFired;
    }

    private sealed class TimedSwordHit
    {
        public TrackedSword Tracked;
        public int AttackerEntityId;
        public int TargetEntityId;
        public float StartTimeSec;
        public float HitAtSec;
        public bool Fired;

        public TimedSwordHit(TrackedSword tracked, int attackerEntityId, int targetEntityId, float startTimeSec, float hitAtSec)
        {
            Tracked = tracked;
            AttackerEntityId = attackerEntityId;
            TargetEntityId = targetEntityId;
            StartTimeSec = startTimeSec;
            HitAtSec = hitAtSec;
            Fired = false;
        }
    }

    public static SwordCollisionService Instance { get; private set; }

    public LayerMask _entityMask;
    private readonly List<TimedSwordHit> _activeTimedHits = new List<TimedSwordHit>();
    private readonly Dictionary<Transform, HashSet<int>> _hitRegistry = new Dictionary<Transform, HashSet<int>>();
    private readonly Dictionary<int, AttackTimingContext> _attackContextsByEntityId = new Dictionary<int, AttackTimingContext>();

    public event Action<Transform, Transform, Vector3, Vector3> OnHitCollider;

    /// <returns>Attack epoch — pass to EndAttack so an older swing cannot clear a newer BeginAttack.</returns>
    public int BeginAttack(int entityId, int targetEntityId, Transform attacker, Transform target, float attackDurationMs, float hitFraction = DEFAULT_HIT_FRACTION)
    {
        if (entityId <= 0) return 0;

        float normalizedDurationMs = attackDurationMs > 0f ? attackDurationMs : DEFAULT_ATTACK_DURATION_MS;
        float normalizedHitFraction = Mathf.Clamp01(hitFraction);
        int epoch = 1;
        if (_attackContextsByEntityId.TryGetValue(entityId, out AttackTimingContext prev))
        {
            epoch = prev.Epoch + 1;
        }

        _attackContextsByEntityId[entityId] = new AttackTimingContext
        {
            EntityId = entityId,
            TargetEntityId = targetEntityId,
            AttackerTransform = attacker,
            TargetTransform = target,
            StartTimeSec = Time.time,
            DurationMs = normalizedDurationMs,
            HitFraction = normalizedHitFraction,
            LastElapsedMs = 0f,
            Epoch = epoch,
            AttackShotFired = false
        };

        string attackerName = attacker != null ? attacker.name : "null";
        string targetName = target != null ? target.name : "null";
        Debug.Log(
            $"{CHAIN_LOG} 1.BeginAttack EVENT_BASED entityId={entityId} epoch={epoch} frame={Time.frameCount} t={Time.time:F3} " +
            $"attacker={attackerName} target={targetName} durationMs={normalizedDurationMs:F1} " +
            $"(melee Hit waits for AttackShot anim event; DurationMs kept for jAtk cycle / logs)");
        Debug.Log($"{TIMER_LOG} BeginAttack entityId={entityId} attacker={attackerName} target={targetName} durationMs={normalizedDurationMs:F1} hitFraction={normalizedHitFraction:F2}");
        return epoch;
    }

    public int BeginAttack(int entityId, Transform attacker, Transform target, float attackDurationMs)
    {
        return BeginAttack(entityId, 0, attacker, target, attackDurationMs);
    }

    public int GetAttackEpoch(int entityId)
    {
        if (entityId <= 0) return 0;
        return _attackContextsByEntityId.TryGetValue(entityId, out AttackTimingContext context) ? context.Epoch : 0;
    }

    public void UpdateAttackProgress(int entityId, float elapsedMs)
    {
        if (entityId <= 0) return;
        if (!_attackContextsByEntityId.TryGetValue(entityId, out AttackTimingContext context)) return;
        if (elapsedMs < 0f) return;
        context.LastElapsedMs = elapsedMs;
    }

    public void EndAttack(int entityId, int epoch = 0)
    {
        if (entityId <= 0) return;
        if (!_attackContextsByEntityId.TryGetValue(entityId, out AttackTimingContext context)) return;
        if (epoch > 0 && context.Epoch != epoch)
        {
            Debug.Log(
                $"{CHAIN_LOG} EndAttack SKIP entityId={entityId} exitEpoch={epoch} liveEpoch={context.Epoch} " +
                $"(older swing must not clear newer BeginAttack)");
            return;
        }

        _attackContextsByEntityId.Remove(entityId);
    }

    /// <summary>
    /// Melee Hit/SoulShot from Unity Animation Event AttackShot (L2 AnimNotify_AttackShot).
    /// </summary>
    public void EmitHitFromAttackShot(
        int attackerEntityId,
        int targetEntityId,
        Transform swordBase,
        Transform swordTip,
        Transform target)
    {
        if (attackerEntityId <= 0 || swordBase == null || swordTip == null || target == null)
        {
            Debug.LogWarning($"{CHAIN_LOG} EmitHitFromAttackShot SKIP bad args attackerId={attackerEntityId}");
            return;
        }

        if (!_attackContextsByEntityId.TryGetValue(attackerEntityId, out AttackTimingContext ctx))
        {
            Debug.LogWarning(
                $"{CHAIN_LOG} EmitHitFromAttackShot SKIP no BeginAttack context attackerId={attackerEntityId}");
            return;
        }

        if (ctx.AttackShotFired)
        {
            Debug.Log(
                $"{CHAIN_LOG} EmitHitFromAttackShot SKIP already fired epoch={ctx.Epoch} attackerId={attackerEntityId}");
            return;
        }

        ctx.AttackShotFired = true;
        if (targetEntityId > 0)
        {
            ctx.TargetEntityId = targetEntityId;
        }

        ResetHitRegistry(swordBase);
        var tracked = new TrackedSword(swordBase, swordTip, target, 0f);
        var timed = new TimedSwordHit(tracked, attackerEntityId, ctx.TargetEntityId, Time.time, Time.time);
        EmitTimedHit(timed);

        Debug.Log(
            $"{CHAIN_LOG} 3.EmitHitFromAttackShot frame={Time.frameCount} t={Time.time:F3} " +
            $"attackerId={attackerEntityId} epoch={ctx.Epoch} sword={swordBase.name} target={target.name}");
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _entityMask = LayerMask.GetMask("EntityClick");
    }

    public void RegisterSword(Transform swordBase, Transform swordTip, Transform target, float extraRange)
    {
        Debug.LogWarning($"{TIMER_LOG} RegisterSword without entity ids is deprecated. Use RegisterSwordByEntityId.");
        RegisterSwordByEntityId(0, 0, swordBase, swordTip, target, extraRange);
    }

    public void RegisterSwordByEntityId(int attackerEntityId, int targetEntityId, Transform swordBase, Transform swordTip, Transform target, float extraRange)
    {
        if (swordBase == null || swordTip == null) return;
        if (target == null)
        {
            Debug.LogWarning("SwordCollisionService: RegisterSword target is null");
            return;
        }

        // --- OLD wall-clock melee Hit (DurationMs * HitFraction). Disabled — use AttackShot. ---
        // if (_activeTimedHits.Exists(s => s.Tracked.basePt == swordBase))
        // {
        //     ResetHitRegistry(swordBase);
        //     RecreateTimedHit(attackerEntityId, targetEntityId, swordBase, swordTip, target, extraRange);
        //     return;
        // }
        //
        // ResetHitRegistry(swordBase);
        // CreateTimedHit(attackerEntityId, targetEntityId, swordBase, swordTip, target, extraRange);

        Debug.LogWarning(
            $"{CHAIN_LOG} RegisterSwordByEntityId ignored (melee Hit is AttackShot-only). " +
            $"attackerId={attackerEntityId} sword={swordBase.name}");
    }

    public void ResetHitRegistry(Transform swordBase)
    {
        if (!_hitRegistry.ContainsKey(swordBase)) _hitRegistry[swordBase] = new HashSet<int>();
        _hitRegistry[swordBase].Clear();
    }

    public void UnregisterSword(Transform swordBase)
    {
        int idx = _activeTimedHits.FindIndex(s => s.Tracked.basePt == swordBase);
        if (idx >= 0)
        {
            _activeTimedHits.RemoveAt(idx);
            _hitRegistry.Remove(swordBase);
        }
    }

    private void LateUpdate()
    {
        // --- OLD wall-clock Hit fire. Disabled — melee uses EmitHitFromAttackShot. ---
        // if (_activeTimedHits.Count == 0) return;
        //
        // float now = Time.time;
        // for (int i = _activeTimedHits.Count - 1; i >= 0; i--)
        // {
        //     TimedSwordHit timed = _activeTimedHits[i];
        //     Transform swordBase = timed.Tracked.basePt;
        //     Transform target = timed.Tracked.target;
        //     if (swordBase == null || target == null)
        //     {
        //         UnregisterSword(swordBase);
        //         continue;
        //     }
        //
        //     if (timed.Fired || now < timed.HitAtSec) continue;
        //
        //     EmitTimedHit(timed);
        //     timed.Fired = true;
        //     UnregisterSword(swordBase);
        // }
    }

    private bool RegisterHit(Transform swordBase, int targetId)
    {
        if (!_hitRegistry.ContainsKey(swordBase)) _hitRegistry[swordBase] = new HashSet<int>();
        if (_hitRegistry[swordBase].Contains(targetId)) return false;

        _hitRegistry[swordBase].Add(targetId);
        return true;
    }

    // --- OLD wall-clock schedule helpers (kept for reference / quick rollback). ---
    // private void CreateTimedHit(int attackerEntityId, int targetEntityId, Transform swordBase, Transform swordTip, Transform target, float extraRange)
    // {
    //     float now = Time.time;
    //     float delaySec = ResolveHitDelaySec(swordBase, attackerEntityId, targetEntityId, extraRange);
    //     var tracked = new TrackedSword(swordBase, swordTip, target, extraRange);
    //     _activeTimedHits.Add(new TimedSwordHit(tracked, attackerEntityId, targetEntityId, now, now + delaySec));
    //     ...
    // }
    //
    // private void RecreateTimedHit(...)
    // private float ResolveHitDelaySec(...)

    private void EmitTimedHit(TimedSwordHit timed)
    {
        Transform swordBase = timed.Tracked.basePt;
        Transform target = timed.Tracked.target;
        if (swordBase == null || target == null) return;

        Entity attackerEntity = GetEntityById(timed.AttackerEntityId);
        Entity targetEntity = GetEntityById(timed.TargetEntityId);
        Transform attackerAnchor = attackerEntity != null ? attackerEntity.transform : swordBase;
        Transform targetAnchor = targetEntity != null ? targetEntity.transform : target;
        Transform hitAnchor = HitAnchorResolver.ResolveHitAnchor(targetEntity, targetAnchor);
        int targetId = targetAnchor.GetInstanceID();
        if (!RegisterHit(swordBase, targetId)) return;

        Vector3 hitPoint = hitAnchor != null ? hitAnchor.position : targetAnchor.position;
        Vector3 hitDirection = VectorUtils.CalcHitDirection(hitPoint, swordBase.position);

        OnHitCollider?.Invoke(attackerAnchor, targetAnchor, hitPoint, hitDirection);

        float now = Time.time;
        float sinceRegisterMs = (now - timed.StartTimeSec) * 1000f;
        float sinceBeginMs = -1f;
        if (timed.AttackerEntityId > 0 &&
            _attackContextsByEntityId.TryGetValue(timed.AttackerEntityId, out AttackTimingContext ctx))
        {
            sinceBeginMs = (now - ctx.StartTimeSec) * 1000f;
        }

        Debug.Log(
            $"{CHAIN_LOG} 4.EmitTimedHit frame={Time.frameCount} t={now:F3} " +
            $"attacker={attackerAnchor.name} target={targetAnchor.name} " +
            $"sinceBeginMs={sinceBeginMs:F1} sinceRegisterMs={sinceRegisterMs:F1} " +
            $"hitPoint={hitPoint} (SoulShot follows AttackShot)");
        Debug.Log($"{TIMER_LOG} Hit attacker={attackerAnchor.name} target={targetAnchor.name} elapsedMs={sinceRegisterMs:F1} hitPoint={hitPoint}");
    }

    private Entity GetEntityById(int entityId)
    {
        if (entityId <= 0 || World.Instance == null) return null;
        return World.Instance.GetEntityNoLockSync(entityId);
    }

}
