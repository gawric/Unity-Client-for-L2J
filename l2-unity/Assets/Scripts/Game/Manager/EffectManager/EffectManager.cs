using UnityEngine;
using VContainer;
using VContainer.Unity;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance;
    public EffectDatabase database;
    [SerializeField] private Transform _activeEffectsContainer;
    [Inject] IObjectResolver _container;
    private const string IMPACT_DEBUG_TAG = "[HIT_DEBUG]";
    private int _impactSpawnCounter = 0;
    private readonly System.Collections.Generic.Dictionary<string, (int count, float windowStartSec)> _impactWindowByPoint
        = new System.Collections.Generic.Dictionary<string, (int count, float windowStartSec)>();
    private const float IMPACT_WINDOW_SEC = 0.35f;

    void Awake() => Instance = this;


    public void PlayEffect(int id, Transform target, MagicCastData castData = null)
    {
        var data = database.effects.Find(e => e.id == id);

        if (data == null || data.prefab == null || _activeEffectsContainer == null || target == null)
        {
            Debug.LogWarning($"EffectManager: PlayEffect data == null || data.prefab == null || _activeEffectsContainer == null || target == null");
            return;
        }

        Debug.Log(
            $"[HOME_SPAWN] EffectManager.PlayEffect id={id} prefab='{data.prefab.name}' " +
            $"target='{target.name}' targetPos={target.position} " +
            $"castTargetId={(castData != null ? castData.TargetObjectId : 0)}");

        BaseEffect instance = Instantiate(data.prefab, target.position, target.rotation, target);
        InjectSpawned(instance);

        instance.gameObject.SetActive(true);
        instance.Setup(data.settings, castData, target);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        EffectDoublePlayLog.TrackManagerPlay(id, data.prefab != null ? data.prefab.name : "null");
#endif
        instance.Play();
    }

    /// <summary>
    /// Melee skill FX attached to weapon (fallback: entity). Builds cast data with
    /// <see cref="MagicCastData.SkillAnimationDuration"/> from SpAtk wall-time so composites
    /// with Match Lifetime To Skill Animation end with the swing.
    /// </summary>
    public void PlayEffectSyncedToSkillAnimation(
        int effectId,
        Entity entity,
        int hitTimeMs,
        AnimationCombo animCombo)
    {
        if (entity == null)
        {
            Debug.LogWarning($"[SKILL_ANIM_FX] PlayEffectSyncedToSkillAnimation entity is null effectId={effectId}");
            return;
        }

        Transform weapon = entity.GetWeaponTransform();

        Transform attach = weapon != null ? weapon : entity.transform;
        MagicCastData castData = SkillAnimationCastDataBuilder.Build(entity, hitTimeMs, animCombo);
        PlayEffect(effectId, attach, castData);
    }

    // L2 Action_Attack: FVector::Rotation(targetLoc - attackerLoc) on XZ.
    // shot_N_atk travel = local +X. LookRotation aligns +Z; yaw -90 maps +X onto dir
    // (yaw +90 was mapping +X onto -dir — cone flew back toward the player).
    private const float IMPACT_HIT_DIRECTION_YAW_OFFSET_DEGREES = -90f;

    public void PlayerImpactEffect(int id, Vector3 point, MagicCastData castData = null)
    {
        PlayerImpactEffect(id, point, Vector3.zero, castData);
    }

    public void PlayerImpactEffect(int id, Vector3 point, Vector3 impactDirection, MagicCastData castData = null)
    {
        Debug.Log(
            $"[HIT_FX] 7.EffectManager.PlayerImpactEffect ENTER frame={Time.frameCount} t={Time.time:F3} " +
            $"effectId={id} point={point} dir={impactDirection}");

        var data = database != null ? database.effects.Find(e => e.id == id) : null;

        if (data == null || data.prefab == null || _activeEffectsContainer == null)
        {
            Debug.LogWarning(
                $"[HIT_FX] 7.EffectManager SKIP Play failed effectId={id} " +
                $"dataNull={data == null} prefabNull={data == null || data.prefab == null} " +
                $"containerNull={_activeEffectsContainer == null}");
            return;
        }

        Vector3 dir = ResolveImpactDirection(point, impactDirection);
        Quaternion rotation = Quaternion.LookRotation(dir, Vector3.up) *
            Quaternion.Euler(0f, IMPACT_HIT_DIRECTION_YAW_OFFSET_DEGREES, 0f);

        GameObject dummy = new GameObject("HitPointProxy");
        dummy.transform.SetPositionAndRotation(point, rotation);
        if (_activeEffectsContainer != null)
        {
            dummy.transform.SetParent(_activeEffectsContainer, true);
        }

        BaseEffect instance = Instantiate(data.prefab, point, rotation, dummy.transform);
        InjectSpawned(instance);
        _impactSpawnCounter += 1;
        string pointKey = $"{Mathf.Round(point.x * 10f) / 10f},{Mathf.Round(point.y * 10f) / 10f},{Mathf.Round(point.z * 10f) / 10f}";
        float now = Time.time;
        if (_impactWindowByPoint.TryGetValue(pointKey, out var state))
        {
            if (now - state.windowStartSec > IMPACT_WINDOW_SEC)
            {
                state = (0, now);
            }
            state.count += 1;
            _impactWindowByPoint[pointKey] = state;
        }
        else
        {
            _impactWindowByPoint[pointKey] = (1, now);
            state = _impactWindowByPoint[pointKey];
        }
        Vector3 emitterRight = rotation * Vector3.right;
        Vector3 emitterFwd = rotation * Vector3.forward;
        Vector3 playerToHit = Vector3.zero;
        Vector3 playerFwd = Vector3.zero;
        if (PlayerEntity.Instance != null)
        {
            playerToHit = hitPointFlat(point - PlayerEntity.Instance.transform.position);
            playerFwd = hitPointFlat(PlayerEntity.Instance.transform.forward);
        }

        float angDirVsPlayerToHit = AngleFlatDeg(dir, playerToHit);
        Debug.Log(
            $"{IMPACT_DEBUG_TAG} PlayerImpactEffect spawn#{_impactSpawnCounter} effectId={id} " +
            $"point={point} dirIn={impactDirection} dirResolved={dir} " +
            $"emitterRight(+X travel)={emitterRight} emitterFwd={emitterFwd} " +
            $"playerToHit={playerToHit} playerFwd={playerFwd} angDirVsPlayerToHit={angDirVsPlayerToHit:F1} " +
            $"yawOffset={IMPACT_HIT_DIRECTION_YAW_OFFSET_DEGREES} " +
            $"countNearPointWindow={state.count} windowSec={IMPACT_WINDOW_SEC:F2}");

        instance.gameObject.SetActive(true);
        instance.Setup(data.settings, castData, dummy.transform);
        // Child parts often use inheritRotation=0 / CasterCenter — push dir into composite context
        // so ResolvePartSpawnRotation orients meshes even when proxy rotation would be ignored.
        if (instance is CompositePrefabEffect composite)
        {
            composite.SetImpactHit(point, dir);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        EffectDoublePlayLog.TrackManagerPlay(id, data.prefab != null ? data.prefab.name : "null");
#endif
        instance.Play();
        Debug.Log(
            $"[HIT_FX] 7.EffectManager PLAY OK spawn#{_impactSpawnCounter} effectId={id} " +
            $"prefab={data.prefab.name} point={point}");
    }

    private static Vector3 hitPointFlat(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.zero;
    }

    private static float AngleFlatDeg(Vector3 a, Vector3 b)
    {
        Vector3 af = hitPointFlat(a);
        Vector3 bf = hitPointFlat(b);
        if (af.sqrMagnitude < 0.0001f || bf.sqrMagnitude < 0.0001f)
        {
            return -1f;
        }

        return Vector3.Angle(af, bf);
    }

    private static Vector3 ResolveImpactDirection(Vector3 hitPoint, Vector3 impactDirection)
    {
        // Always flatten: sword hitDirection carries pitch and tilts the emitter.
        if (impactDirection.sqrMagnitude > 0.0001f)
        {
            Vector3 flat = impactDirection;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.0001f)
            {
                return flat.normalized;
            }
        }

        // L2: targetLoc - attackerLoc (horizontal).
        if (PlayerEntity.Instance != null)
        {
            Vector3 fromAttacker = hitPoint - PlayerEntity.Instance.transform.position;
            fromAttacker.y = 0f;
            if (fromAttacker.sqrMagnitude > 0.0001f)
            {
                return fromAttacker.normalized;
            }
        }

        return Vector3.forward;
    }

    void InjectSpawned(BaseEffect instance)
    {
        if (instance == null || _container == null)
        {
            return;
        }

        _container.InjectGameObject(instance.gameObject);
    }
}