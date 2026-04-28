using System;
using System.Collections.Generic;
using UnityEngine;

public class SwordCollisionService : MonoBehaviour
{
    private const string TIMER_LOG = "[SWORD_TIMER]";
    private const float DEFAULT_HIT_FRACTION = 0.88f;
    private const float DEFAULT_HIT_DELAY_SEC = 0.3f;
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

    public void BeginAttack(int entityId, int targetEntityId, Transform attacker, Transform target, float attackDurationMs, float hitFraction = DEFAULT_HIT_FRACTION)
    {
        if (entityId <= 0) return;

        float normalizedDurationMs = attackDurationMs > 0f ? attackDurationMs : DEFAULT_ATTACK_DURATION_MS;
        float normalizedHitFraction = Mathf.Clamp01(hitFraction);
        _attackContextsByEntityId[entityId] = new AttackTimingContext
        {
            EntityId = entityId,
            TargetEntityId = targetEntityId,
            AttackerTransform = attacker,
            TargetTransform = target,
            StartTimeSec = Time.time,
            DurationMs = normalizedDurationMs,
            HitFraction = normalizedHitFraction,
            LastElapsedMs = 0f
        };

        string attackerName = attacker != null ? attacker.name : "null";
        string targetName = target != null ? target.name : "null";
        Debug.Log($"{TIMER_LOG} BeginAttack entityId={entityId} attacker={attackerName} target={targetName} durationMs={normalizedDurationMs:F1} hitFraction={normalizedHitFraction:F2}");
    }

    public void BeginAttack(int entityId, Transform attacker, Transform target, float attackDurationMs)
    {
        BeginAttack(entityId, 0, attacker, target, attackDurationMs);
    }

    public void UpdateAttackProgress(int entityId, float elapsedMs)
    {
        if (entityId <= 0) return;
        if (!_attackContextsByEntityId.TryGetValue(entityId, out AttackTimingContext context)) return;
        if (elapsedMs < 0f) return;
        context.LastElapsedMs = elapsedMs;
    }

    public void EndAttack(int entityId)
    {
        if (entityId <= 0) return;
        _attackContextsByEntityId.Remove(entityId);
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

        if (_activeTimedHits.Exists(s => s.Tracked.basePt == swordBase))
        {
            ResetHitRegistry(swordBase);
            RecreateTimedHit(attackerEntityId, targetEntityId, swordBase, swordTip, target, extraRange);
            return;
        }

        ResetHitRegistry(swordBase);
        CreateTimedHit(attackerEntityId, targetEntityId, swordBase, swordTip, target, extraRange);
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
        if (_activeTimedHits.Count == 0) return;

        float now = Time.time;
        for (int i = _activeTimedHits.Count - 1; i >= 0; i--)
        {
            TimedSwordHit timed = _activeTimedHits[i];
            Transform swordBase = timed.Tracked.basePt;
            Transform target = timed.Tracked.target;
            if (swordBase == null || target == null)
            {
                UnregisterSword(swordBase);
                continue;
            }

            if (timed.Fired || now < timed.HitAtSec) continue;

            EmitTimedHit(timed);
            timed.Fired = true;
            UnregisterSword(swordBase);
        }
    }

    private bool RegisterHit(Transform swordBase, int targetId)
    {
        if (!_hitRegistry.ContainsKey(swordBase)) _hitRegistry[swordBase] = new HashSet<int>();
        if (_hitRegistry[swordBase].Contains(targetId)) return false;

        _hitRegistry[swordBase].Add(targetId);
        return true;
    }

    private void CreateTimedHit(int attackerEntityId, int targetEntityId, Transform swordBase, Transform swordTip, Transform target, float extraRange)
    {
        float now = Time.time;
        float delaySec = ResolveHitDelaySec(swordBase, attackerEntityId, targetEntityId, extraRange);
        var tracked = new TrackedSword(swordBase, swordTip, target, extraRange);
        _activeTimedHits.Add(new TimedSwordHit(tracked, attackerEntityId, targetEntityId, now, now + delaySec));
        Debug.Log($"{TIMER_LOG} Register attacker={swordBase.name} target={target.name} delayMs={(delaySec * 1000f):F1}");
    }

    private void RecreateTimedHit(int attackerEntityId, int targetEntityId, Transform swordBase, Transform swordTip, Transform target, float extraRange)
    {
        int idx = _activeTimedHits.FindIndex(s => s.Tracked.basePt == swordBase);
        if (idx >= 0)
        {
            _activeTimedHits.RemoveAt(idx);
        }
        CreateTimedHit(attackerEntityId, targetEntityId, swordBase, swordTip, target, extraRange);
    }

    private float ResolveHitDelaySec(Transform swordBase, int attackerEntityId, int targetEntityId, float extraRange)
    {
        // Keep API-compatible override path: caller may pass custom delay in seconds.
        if (extraRange > 0f) return extraRange;
        if (swordBase == null) return DEFAULT_HIT_DELAY_SEC;

        if (attackerEntityId > 0 && _attackContextsByEntityId.TryGetValue(attackerEntityId, out AttackTimingContext context))
        {
            if (targetEntityId > 0) context.TargetEntityId = targetEntityId;

            float hitMomentMs = context.DurationMs * context.HitFraction;
            float elapsedMs = Mathf.Max(0f, Mathf.Max(context.LastElapsedMs, (Time.time - context.StartTimeSec) * 1000f));
            float remainingMs = Mathf.Max(0f, hitMomentMs - elapsedMs);
            return TimeUtils.ConvertMsToSec(remainingMs);
        }

        return TimeUtils.ConvertMsToSec(DEFAULT_ATTACK_DURATION_MS * DEFAULT_HIT_FRACTION);
    }

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

        float elapsedMs = (Time.time - timed.StartTimeSec) * 1000f;
        Debug.Log($"{TIMER_LOG} Hit attacker={attackerAnchor.name} target={targetAnchor.name} elapsedMs={elapsedMs:F1} hitPoint={hitPoint}");
    }

    private Entity GetEntityById(int entityId)
    {
        if (entityId <= 0 || World.Instance == null) return null;
        return World.Instance.GetEntityNoLockSync(entityId);
    }

}
