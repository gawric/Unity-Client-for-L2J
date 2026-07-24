using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime pool for effect hit flashes (L2 FNPawnLight-style).
/// Visual params come from LightEffectSetting when provided.
/// </summary>
public class FNManagerLight : MonoBehaviour
{
    public static FNManagerLight Instance { get; private set; }
    private const string LOG_TAG = "[FN_LIGHT]";

    [Header("Fallback defaults (used when settings == null)")]
    [SerializeField] private Color _color = Color.white;
    [SerializeField] private float _intensity = 0.6f;
    [SerializeField] private float _durationSeconds = 0.4f;
    [SerializeField] private float _rangeMeters = 2f;
    [SerializeField] private float _spotAngle = 71.5f;
    [SerializeField] private float _innerSpotAngle = 32.5f;
    [SerializeField] private int _poolSize = 8;
    [SerializeField] private bool _debugLog = true;

    private readonly List<FlashSlot> _slots = new List<FlashSlot>();
    private Transform _poolRoot;
    private int _spawnCounter;

    private sealed class FlashSlot
    {
        public GameObject Go;
        public Light Light;
        public float Age;
        public float Duration;
        public float PeakIntensity;
        public bool Active;
        public int SpawnId;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (_debugLog)
            {
                Debug.LogWarning($"{LOG_TAG} duplicate on '{name}' — Destroy(this), keep existing Instance");
            }

            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsurePool();
        if (_debugLog)
        {
            Debug.Log(
                $"{LOG_TAG} Awake ok on '{name}' id={GetInstanceID()} " +
                $"fallback intensity={_intensity:F2} range={_rangeMeters:F3}m duration={_durationSeconds:F3}s pool={_poolSize}");
        }
    }

    public static FNManagerLight Ensure()
    {
        if (Instance != null)
        {
            return Instance;
        }

        Debug.LogWarning($"{LOG_TAG} Instance was null — creating runtime GameObject");
        GameObject go = new GameObject(nameof(FNManagerLight));
        return go.AddComponent<FNManagerLight>();
    }

    public void SpawnHitFlash(Vector3 worldPosition, Vector3 aimDirection)
    {
        SpawnHitFlash(worldPosition, aimDirection, null);
    }

    public void SpawnHitFlash(Vector3 worldPosition, Vector3 aimDirection, LightEffectSetting settings)
    {
        EnsurePool();
        FlashSlot slot = AcquireSlot();
        if (slot == null || slot.Light == null)
        {
            Debug.LogError($"{LOG_TAG} SpawnHitFlash FAILED — no pool slot / Light null");
            return;
        }

        Vector3 dir = aimDirection;
        if (dir.sqrMagnitude > 0.0001f)
        {
            dir.Normalize();
            slot.Go.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
        else
        {
            dir = Vector3.forward;
            slot.Go.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        Color color = settings != null ? settings.color : _color;
        float intensity = settings != null ? settings.intensity : _intensity;
        float duration = settings != null ? settings.durationSeconds : _durationSeconds;
        float range = settings != null ? settings.rangeMeters : _rangeMeters;
        float spotAngle = settings != null ? settings.spotAngle : _spotAngle;
        float innerSpot = settings != null ? settings.innerSpotAngle : _innerSpotAngle;

        _spawnCounter += 1;
        slot.SpawnId = _spawnCounter;
        slot.Go.transform.position = worldPosition;
        slot.Age = 0f;
        slot.Duration = Mathf.Max(0.05f, duration);
        slot.PeakIntensity = Mathf.Max(0.01f, intensity);
        slot.Light.type = LightType.Spot;
        slot.Light.spotAngle = Mathf.Clamp(spotAngle, 1f, 179f);
        slot.Light.innerSpotAngle = Mathf.Clamp(innerSpot, 0f, slot.Light.spotAngle);
        slot.Light.color = color;
        slot.Light.range = Mathf.Max(0.05f, range);
        slot.Light.intensity = slot.PeakIntensity;
        slot.Light.enabled = true;
        slot.Active = true;
        slot.Go.SetActive(true);

        if (_debugLog)
        {
            string settingsName = settings != null ? settings.name : "<fallback>";
            Debug.Log(
                $"{LOG_TAG} SPAWN #{slot.SpawnId} settings={settingsName} type=Spot pos={worldPosition} dir={dir} " +
                $"intensity={slot.Light.intensity:F2} range={slot.Light.range:F3}m " +
                $"spotAngle={slot.Light.spotAngle:F0} duration={slot.Duration:F3}s " +
                $"activeSlots={CountActive()}/{_slots.Count}");
        }
    }

    private int CountActive()
    {
        int n = 0;
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].Active)
            {
                n++;
            }
        }

        return n;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < _slots.Count; i++)
        {
            FlashSlot slot = _slots[i];
            if (!slot.Active)
            {
                continue;
            }

            slot.Age += dt;
            float t = slot.Age / slot.Duration;
            if (t >= 1f)
            {
                if (_debugLog)
                {
                    Debug.Log($"{LOG_TAG} RELEASE #{slot.SpawnId} after {slot.Age:F3}s");
                }

                ReleaseSlot(slot);
                continue;
            }

            float envelope = 1f - t;
            envelope *= envelope;
            slot.Light.intensity = slot.PeakIntensity * envelope;
        }
    }

    private void EnsurePool()
    {
        if (_poolRoot == null)
        {
            GameObject root = new GameObject("FNPawnLightPool");
            root.transform.SetParent(transform, false);
            _poolRoot = root.transform;
        }

        while (_slots.Count < _poolSize)
        {
            _slots.Add(CreateSlot(_slots.Count));
        }
    }

    private FlashSlot CreateSlot(int index)
    {
        GameObject go = new GameObject($"FNPawnLight_{index}");
        go.transform.SetParent(_poolRoot, false);
        Light light = go.AddComponent<Light>();
        light.type = LightType.Spot;
        light.spotAngle = _spotAngle;
        light.innerSpotAngle = _innerSpotAngle;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForcePixel;
        light.enabled = true;
        go.SetActive(false);

        return new FlashSlot
        {
            Go = go,
            Light = light,
            Active = false
        };
    }

    private FlashSlot AcquireSlot()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            if (!_slots[i].Active)
            {
                return _slots[i];
            }
        }

        FlashSlot oldest = _slots[0];
        float maxAge = oldest.Age;
        for (int i = 1; i < _slots.Count; i++)
        {
            if (_slots[i].Age > maxAge)
            {
                maxAge = _slots[i].Age;
                oldest = _slots[i];
            }
        }

        ReleaseSlot(oldest);
        return oldest;
    }

    private static void ReleaseSlot(FlashSlot slot)
    {
        slot.Active = false;
        slot.Age = 0f;
        if (slot.Light != null)
        {
            slot.Light.intensity = 0f;
        }

        if (slot.Go != null)
        {
            slot.Go.SetActive(false);
        }
    }
}
