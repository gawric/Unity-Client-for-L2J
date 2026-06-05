using System.Collections;
using UnityEngine;

/// <summary>
/// Moves child transforms one after another along up/down (staggered "falling rings").
/// Put on the parent group (e.g. windAround); targets default to direct children.
/// Speed is always real world meters/sec (works inside scaled effect prefabs).
/// </summary>
[DisallowMultipleComponent]
public sealed class EffectPrefabStaggeredDrift : MonoBehaviour
{
    public enum DriftDirection
    {
        Down = 0,
        Up = 1,
    }

    [Header("Targets")]
    [Tooltip("Empty = all direct child transforms (in hierarchy order).")]
    [SerializeField] private Transform[] _targets;
    [Tooltip("Must be on when ParticleGroup disables renderer slots between bursts.")]
    [SerializeField] private bool _includeInactiveChildren = true;

    [Header("Motion")]
    [SerializeField] private DriftDirection _direction = DriftDirection.Down;
    [SerializeField] private float _speedMetersPerSecond = 0.2f;
    [Tooltip("0 = no limit.")]
    [SerializeField] private float _maxDriftMeters;
    [SerializeField] private Space _driftSpace = Space.Self;

    [Header("Stagger")]
    [SerializeField] private float _startDelaySeconds;
    [Tooltip("Delay before each next target starts drifting.")]
    [SerializeField] private float _staggerDelaySeconds = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool _debugLog = true;
    [SerializeField] private float _debugLogIntervalSeconds = 0.25f;
    [SerializeField] private float _accelerationMetersPerSecond2 = 1.0f; // Ускорение движения

    private const string LogTag = "[CURE_DRIFT]";

    private Transform[] _resolvedTargets;
    private Vector3[] _initialLocalPositions;
    private Vector3[] _initialWorldPositions;
    private float[] _driftMeters;
    private float _enabledAt = -1f;
    private float _nextDebugLogAt;
    private Coroutine _deferredRestartRoutine;

    private void OnEnable()
    {
        _enabledAt = Time.time; // Фиксируем время физического появления
        _nextDebugLogAt = 0f;
        Restart();
        ScheduleDeferredRestart();
        LogState("OnEnable");
    }

    private void Start()
    {
        _enabledAt = Time.time; // Фиксируем время на случай отложенного Start
        Restart();
        LogState("Start");
    }

    private void OnDisable()
    {
        if (_deferredRestartRoutine != null)
        {
            StopCoroutine(_deferredRestartRoutine);
            _deferredRestartRoutine = null;
        }

        RestoreInitialPositions();
    }

    public void Restart()
    {
        Transform[] newTargets = ResolveTargets();
        int count = newTargets != null ? newTargets.Length : 0;
        if (count == 0)
        {
            if (_debugLog)
            {
                Debug.LogWarning(
                    $"{LogTag} '{name}' Restart: no targets (children={transform.childCount}, assigned={(_targets != null ? _targets.Length : 0)}, includeInactive={_includeInactiveChildren}) — keeping previous list.",
                    this);
            }
            return;
        }

        _resolvedTargets = newTargets;

        // Инициализируем массивы строго один раз при первом спавне
        if (_initialLocalPositions == null || _initialLocalPositions.Length != count)
        {
            _initialLocalPositions = new Vector3[count];
            _initialWorldPositions = new Vector3[count];
            _driftMeters = new float[count];

            for (int i = 0; i < count; i++)
            {
                Transform t = _resolvedTargets[i];
                if (t == null) continue;

                _initialLocalPositions[i] = t.localPosition;
                _driftMeters[i] = 0f;
            }
        }

        // Рассчитываем, сколько времени система УЖЕ реально движется к моменту этого вызова Restart()
        float systemElapsed = Time.time - _enabledAt;

        for (int i = 0; i < count; i++)
        {
            Transform t = _resolvedTargets[i];
            if (t == null) continue;

            // Временно возвращаем в локальный базис, чтобы Unity корректно 
            // применил новый lossyScale (0.09 вместо 0.07) от внешней системы
            t.localPosition = _initialLocalPositions[i];
            _initialWorldPositions[i] = t.position;

            // ИСПРАВЛЕНИЕ: Вычисляем правильный driftMeters для текущего кадра корутины,
            // вместо того чтобы грубо обнулять его в 0f!
            float targetStart = _startDelaySeconds + (_staggerDelaySeconds * i);
            if (systemElapsed >= targetStart)
            {
                float localTime = systemElapsed - targetStart;
                float v0 = _speedMetersPerSecond;
                float a = _accelerationMetersPerSecond2;

                // Восстанавливаем пройденную дистанцию на момент вызова
                float currentDrift = (v0 * localTime) + (0.5f * a * localTime * localTime);
                if (_maxDriftMeters > 0f && currentDrift >= _maxDriftMeters)
                {
                    currentDrift = _maxDriftMeters;
                }
                _driftMeters[i] = currentDrift;
            }
            else
            {
                _driftMeters[i] = 0f;
            }
        }

        if (_debugLog)
        {
            Debug.Log(
                $"{LogTag} '{name}' Restart: targets={count} speed={_speedMetersPerSecond:F3}m/s space={_driftSpace} " +
                $"lossyScale={transform.lossyScale} active={gameObject.activeInHierarchy} frame={Time.frameCount}.",
                this);
        }
    }




    private void ScheduleDeferredRestart()
    {
        if (_deferredRestartRoutine != null)
        {
            StopCoroutine(_deferredRestartRoutine);
        }

        _deferredRestartRoutine = StartCoroutine(DeferredRestartRoutine());
    }

    private IEnumerator DeferredRestartRoutine()
    {
        // PlayPart / composite attach can run after our first OnEnable — re-anchor transforms.
        yield return null;
        Restart();
        LogState("Deferred+1");
        yield return null;
        Restart();
        LogState("Deferred+2");
        _deferredRestartRoutine = null;
    }

    private void LateUpdate()
    {
        if (_resolvedTargets == null || _resolvedTargets.Length == 0)
        {
            return;
        }

        // Полное время, прошедшее с момента истинной активации системы
        float elapsed = Time.time - _enabledAt;

        for (int i = 0; i < _resolvedTargets.Length; i++)
        {
            Transform t = _resolvedTargets[i];
            if (t == null) continue;

            // Точный математический момент старта конкретного кольца
            float targetStart = _startDelaySeconds + (_staggerDelaySeconds * i);

            // --- БЛОКИРОВКА СБРОСА ШЕЙДЕРА (ФИКС БАГА С ПОВТОРОМ) ---
            // Вычисляем, какое ИСТИННОЕ стартовое время должно быть у этого кольца в мире Unity
            float correctShaderStartTime = _enabledAt + targetStart;

            // Находим материал кольца и принудительно прописываем ему правильное время старта.
            // Это не даст внешнему скрипту CompositePrefabEffect сбросить анимацию размера.
            if (t.TryGetComponent<MeshRenderer>(out var renderer) && renderer.material != null)
            {
                if (renderer.material.HasProperty("_StartTime"))
                {
                    renderer.material.SetFloat("_StartTime", correctShaderStartTime);
                }
            }

            // Если время этого кольца еще не пришло, удерживаем его на стартовой позиции
            if (elapsed < targetStart)
            {
                t.localPosition = _initialLocalPositions[i];
                continue;
            }

            // --- ГЕОМЕТРИЯ ПАДЕНИЯ ---
            float localTime = elapsed - targetStart;
            float v0 = _speedMetersPerSecond;
            float a = _accelerationMetersPerSecond2;

            // Равноускоренное движение вниз
            float currentDrift = (v0 * localTime) + (0.5f * a * localTime * localTime);

            if (_maxDriftMeters > 0f && currentDrift >= _maxDriftMeters)
            {
                currentDrift = _maxDriftMeters;
            }

            _driftMeters[i] = currentDrift;
            ApplyPosition(t, i, currentDrift);
        }

        if (_debugLog && Time.time >= _nextDebugLogAt)
        {
            _nextDebugLogAt = Time.time + Mathf.Max(0.05f, _debugLogIntervalSeconds);
            LogState("LateUpdate");
        }
    }

    private void ApplyPosition(Transform t, int index, float driftMeters)
    {
        Vector3 worldAxis = GetWorldAxisVector();

        // --- ЛОГИКА ПРОСТРАНСТВЕННОГО НАХЛЁСТА ---
        // Узнаем, какую МАКСИМАЛЬНУЮ дистанцию должно пройти одно кольцо
        // Если мы хотим идеальный стык встык, то начальный сдвиг = _maxDriftMeters * index.
        // Если хотим НАХЛЁСТ (чтобы они немного перекрывали друг друга), умножаем на 0.8 или 0.9.
        float overlapFactor = 0.85f; // 15% нахлёста для бесшовности текстуры
        float chainOffset = _maxDriftMeters * index * overlapFactor;

        // Итоговое смещение = стартовый сдвиг по цепочке + текущее падение по времени
        float totalDrift = chainOffset + driftMeters;

        Vector3 worldOffset = worldAxis * totalDrift;

        if (_driftSpace == Space.World)
        {
            t.position = _initialWorldPositions[index] + worldOffset;
            return;
        }

        Transform parentTransform = t.parent;
        Vector3 localOffset = parentTransform != null
            ? parentTransform.InverseTransformVector(worldOffset)
            : worldOffset;

        t.localPosition = _initialLocalPositions[index] + localOffset;
    }


    private Vector3 GetWorldAxisVector()
    {
        return _direction == DriftDirection.Down ? Vector3.down : Vector3.up;
    }

    private void LogState(string phase)
    {
        if (!_debugLog)
        {
            return;
        }

        float elapsed = _enabledAt > 0f ? Time.time - _enabledAt : 0f;
        int count = _resolvedTargets != null ? _resolvedTargets.Length : 0;
        if (count == 0)
        {
            Debug.Log($"{LogTag} '{name}' {phase}: no targets elapsed={elapsed:F3}s frame={Time.frameCount}.", this);
            return;
        }

        Transform t0 = _resolvedTargets[0];
        float d0 = _driftMeters != null && _driftMeters.Length > 0 ? _driftMeters[0] : 0f;
        Vector3 initLocal = _initialLocalPositions != null && _initialLocalPositions.Length > 0
            ? _initialLocalPositions[0]
            : Vector3.zero;
        Vector3 initWorld = _initialWorldPositions != null && _initialWorldPositions.Length > 0
            ? _initialWorldPositions[0]
            : Vector3.zero;

        Debug.Log(
            $"{LogTag} '{name}' {phase}: elapsed={elapsed:F3}s targets={count} drift0={d0:F4}m " +
            $"t0='{(t0 != null ? t0.name : "null")}' active={(t0 != null && t0.gameObject.activeSelf)} " +
            $"local={FormatV(t0 != null ? t0.localPosition : Vector3.zero)} initLocal={FormatV(initLocal)} " +
            $"world={FormatV(t0 != null ? t0.position : Vector3.zero)} initWorld={FormatV(initWorld)} " +
            $"parentScale={FormatV(transform.lossyScale)} speed={_speedMetersPerSecond:F3} space={_driftSpace} frame={Time.frameCount}.",
            this);
    }

    private static string FormatV(Vector3 v) => $"({v.x:F3},{v.y:F3},{v.z:F3})";

    private Transform[] ResolveTargets()
    {
        if (_targets != null && _targets.Length > 0)
        {
            int valid = 0;
            for (int i = 0; i < _targets.Length; i++)
            {
                if (_targets[i] != null)
                {
                    valid++;
                }
            }

            var assigned = new Transform[valid];
            int idx = 0;
            for (int i = 0; i < _targets.Length; i++)
            {
                if (_targets[i] != null)
                {
                    assigned[idx++] = _targets[i];
                }
            }

            return assigned;
        }

        int childCount = transform.childCount;
        if (childCount == 0)
        {
            return System.Array.Empty<Transform>();
        }

        var list = new System.Collections.Generic.List<Transform>(childCount);
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (!_includeInactiveChildren && !child.gameObject.activeSelf)
            {
                continue;
            }
            list.Add(child);
        }

        return list.ToArray();
    }

    private void RestoreInitialPositions()
    {
        if (_resolvedTargets == null || _initialLocalPositions == null)
        {
            return;
        }

        int count = Mathf.Min(_resolvedTargets.Length, _initialLocalPositions.Length);
        for (int i = 0; i < count; i++)
        {
            Transform t = _resolvedTargets[i];
            if (t == null)
            {
                continue;
            }

            if (_driftSpace == Space.World && _initialWorldPositions != null && i < _initialWorldPositions.Length)
            {
                t.position = _initialWorldPositions[i];
            }
            else
            {
                t.localPosition = _initialLocalPositions[i];
            }
        }
    }



}
