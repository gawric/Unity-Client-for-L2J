using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance;
    public EffectDatabase database;
    [SerializeField] private Transform _activeEffectsContainer;
    private const string IMPACT_DEBUG_TAG = "[HIT_DEBUG]";
    private int _impactSpawnCounter = 0;
    private readonly System.Collections.Generic.Dictionary<string, (int count, float windowStartSec)> _impactWindowByPoint
        = new System.Collections.Generic.Dictionary<string, (int count, float windowStartSec)>();
    private const float IMPACT_WINDOW_SEC = 0.35f;

    void Awake() => Instance = this;


    public void PlayEffect(int id, Transform target, MagicCastData castData = null)
    {
        var data = database.effects.Find(e => e.id == id);

        if (data == null || data.prefab == null || _activeEffectsContainer == null)
        {
            Debug.LogWarning($"EffectManager: PlayEffect data == null || data.prefab == null || _activeEffectsContainer == null");
            return;
        }

        BaseEffect instance = Instantiate(data.prefab, target.position, target.rotation, target);

        instance.gameObject.SetActive(true);
        instance.Setup(data.settings, castData, target);
        instance.Play();
    }

    public void PlayerImpactEffect(int id, Vector3 point, MagicCastData castData = null)
    {
        var data = database.effects.Find(e => e.id == id);

        if (data == null || data.prefab == null || _activeEffectsContainer == null)
        {
            Debug.LogWarning($"EffectManager: PlayEffect data == null || data.prefab == null || _activeEffectsContainer == null");
            return;
        }

        GameObject dummy = new GameObject("HitPointProxy");
        dummy.transform.position = point;

        BaseEffect instance = Instantiate(data.prefab, point, Quaternion.identity, dummy.transform);
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
        Debug.Log($"{IMPACT_DEBUG_TAG} PlayerImpactEffect spawn#{_impactSpawnCounter} effectId={id} point={point} countNearPointWindow={state.count} windowSec={IMPACT_WINDOW_SEC:F2}");

        instance.gameObject.SetActive(true);
        instance.Setup(data.settings, castData, dummy.transform);
        instance.Play();

    }
}