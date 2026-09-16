using System;
using UnityEngine;

[System.Serializable]
public struct WorldTimer {
    public float dayStartTime; //0.25f
    public float dayEndTime; //0.75f
    public float sunriseStartTime; //0f
    public float sunriseEndTime; //0.15f
    public float sunsetStartTime; //0.85f
    public float sunsetEndTime; //0.99f
}

[System.Serializable]
public struct Clock {
    public float totalRatio;
    [Header("Day/Night cycle")]
    public float dayRatio;
    public float nightRatio;
    [Header("Full day cycles")]
    public float dawnRatio;
    public float brightRatio;
    public float duskRatio;
    public float darkRatio;
}

[ExecuteInEditMode]
public class WorldClock : MonoBehaviour {
    [SerializeField] private float _dayDurationMinutes = 2.5f;
    [SerializeField] private string _timeHour;
    [SerializeField] private float _timeElapsed = 0;
    [SerializeField] private bool _startClock = true;
    [SerializeField] private WorldTimer _worldTimer;
    [SerializeField] private Clock _clock;

    public Clock Clock { get { return _clock; } }

    /// <summary>L2 world hour in [0, 24). Maps Clock.totalRatio * 24 (midnight = 0).</summary>
    public float WorldHours
    {
        get { return _clock.totalRatio * 24f; }
    }

    private static WorldClock _instance;
    public static WorldClock Instance { get { return _instance; } }

    bool _persistentDriver;
    static bool _bootstrapping;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        _instance = null;
        _bootstrapping = false;
    }

    /// <summary>
    /// Menu is unloaded on world enter; Game clock lives under World/Environment and can stay disabled.
    /// Keep one DontDestroyOnLoad driver so time always ticks.
    /// </summary>
    public static WorldClock EnsurePersistent()
    {
        if (_instance != null && _instance._persistentDriver)
        {
            _instance.enabled = true;
            _instance.gameObject.SetActive(true);
            _instance._startClock = true;
            _instance._dayDurationMinutes = 2.5f;
            return _instance;
        }

        // DayNightCycle is ExecuteInEditMode — never create DDOL drivers in edit mode.
        if (!Application.isPlaying)
        {
            if (_instance != null)
                return _instance;
            return FindFirstObjectByType<WorldClock>(FindObjectsInactive.Include);
        }

        if (_bootstrapping)
        {
            return _instance;
        }

        WorldClock existing = _instance;
        if (existing == null)
        {
            existing = FindFirstObjectByType<WorldClock>(FindObjectsInactive.Include);
        }

        _bootstrapping = true;
        var go = new GameObject("L2ClockDriver");
        DontDestroyOnLoad(go);
        WorldClock driver = go.AddComponent<WorldClock>();
        driver._persistentDriver = true;
        driver._startClock = true;
        driver._dayDurationMinutes = 2.5f;
        if (existing != null && existing != driver)
        {
            driver._timeElapsed = existing._timeElapsed;
            driver._clock = existing._clock;
            driver._timeHour = existing._timeHour;
        }

        _instance = driver;
        _bootstrapping = false;
        Debug.Log("[L2Clock] persistent driver started (survives Menu unload / map tiles)");
        return driver;
    }

    private void Awake()
    {
        ClaimInstance();
    }

    void OnEnable()
    {
        if (Application.isPlaying)
        {
            _startClock = true;
            if (_dayDurationMinutes < 0.5f || _dayDurationMinutes > 5f)
            {
                _dayDurationMinutes = 2.5f;
            }
        }

        ClaimInstance();
    }

    void ClaimInstance()
    {
        if (_bootstrapping || _persistentDriver)
        {
            _persistentDriver = true;
            _instance = this;
            return;
        }

        if (!Application.isPlaying)
        {
            if (_instance == null)
            {
                _instance = this;
            }

            return;
        }

        EnsurePersistent();
    }

    void OnDisable()
    {
        if (_persistentDriver)
        {
            return;
        }

        if (_instance == this)
        {
            _instance = null;
        }
    }

    void OnDestroy() {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    void Update() {
        if (!_persistentDriver && Application.isPlaying && _instance != null && _instance != this)
        {
            return;
        }

        if(_startClock) {
            UpdateClock();
        }

        CalculateDayNightRatio();
        CalculateSunPhaseRatio();
    }

    private void UpdateClock() {
        _timeElapsed += Time.deltaTime;

        if(_timeElapsed >= (_dayDurationMinutes * 60f)) {
            _timeElapsed = 0;
        }

        _clock.totalRatio = _timeElapsed / (_dayDurationMinutes * 60f);

        // Calculate the number of seconds based on the percentage
        int seconds = (int)(_clock.totalRatio * 86400);

        // Convert seconds to hours, minutes, and seconds
        int hours = seconds / 3600;
        int minutes = (seconds % 3600) / 60;
        int remainingSeconds = seconds % 60;

        // Create a TimeSpan object with the calculated hours, minutes, and seconds
        TimeSpan time = new TimeSpan(hours, minutes, remainingSeconds);

        // Format the TimeSpan object as a string in the desired format (HH:mm:ss)
        _timeHour = time.ToString(@"hh\:mm\:ss");
    }

    public void SetWorldHours(float hours)
    {
        float t = hours % 24f;
        if (t < 0f)
        {
            t += 24f;
        }

        _clock.totalRatio = t / 24f;
        _timeElapsed = _clock.totalRatio * _dayDurationMinutes * 60f;
        int seconds = (int)(_clock.totalRatio * 86400);
        int h = seconds / 3600;
        int m = (seconds % 3600) / 60;
        int s = seconds % 60;
        _timeHour = new TimeSpan(h, m, s).ToString(@"hh\:mm\:ss");
        CalculateDayNightRatio();
        CalculateSunPhaseRatio();
    }

    public void SynchronizeClock(long gameTicks, int tickDurationMs, int dayDurationMinutes) {
        if (_startClock)
        {
            return;
        }

        float ticksPerDay = (float)dayDurationMinutes * 60 * 1000 / tickDurationMs;
        float currentHours = gameTicks / ticksPerDay * 24 % 24;
        this._dayDurationMinutes = dayDurationMinutes;
        float serverDayRatio = currentHours / 24f;
        _timeElapsed = serverDayRatio * dayDurationMinutes * 60f;
    }

    public bool IsNightTime() {
        return _clock.nightRatio > 0 && _clock.nightRatio <= 1 || _clock.dayRatio < 0.25f;
    }

    private void CalculateDayNightRatio() {
        if(_clock.totalRatio >= _worldTimer.dayStartTime && _clock.totalRatio < _worldTimer.dayEndTime) {
            _clock.nightRatio = 0;
            _clock.dayRatio = (_clock.totalRatio - _worldTimer.dayStartTime) / (_worldTimer.dayEndTime - _worldTimer.dayStartTime);
        } else {
            _clock.dayRatio = 0;
            if(_clock.totalRatio >= _worldTimer.dayEndTime) {
                _clock.nightRatio = (_clock.totalRatio - _worldTimer.dayEndTime) / (1.0f - _worldTimer.dayEndTime + _worldTimer.dayStartTime);
            } else {
                _clock.nightRatio = (_clock.totalRatio + (1.0f - _worldTimer.dayEndTime)) / (1.0f - _worldTimer.dayEndTime + _worldTimer.dayStartTime);
            }
        }
    }

    private void CalculateSunPhaseRatio() {
        _clock.dawnRatio = CalculatePeriodRatio(_worldTimer.sunriseStartTime, _worldTimer.sunriseEndTime);
        _clock.brightRatio = CalculatePeriodRatio(_worldTimer.sunriseEndTime, _worldTimer.sunsetStartTime);
        _clock.duskRatio = CalculatePeriodRatio(_worldTimer.sunsetStartTime, _worldTimer.sunsetEndTime);
        _clock.darkRatio = CalculatePeriodRatio(-.99f, _worldTimer.sunriseStartTime);
    }

    private float CalculatePeriodRatio(float startRatio, float endRatio) {
        float periodDuration = (startRatio < 0) ? (Mathf.Abs(startRatio) + endRatio) : (endRatio - startRatio);

        float ratio = 0;
        if(_clock.dayRatio >= 0 && _clock.nightRatio == 0) {
            // Day
            if(_clock.dayRatio <= endRatio) {
                // In Range        
                if(startRatio < 0) {
                    ratio += Mathf.Abs(startRatio);
                    ratio += _clock.dayRatio;
                } else if(_clock.dayRatio >= startRatio) {
                    ratio -= startRatio;
                    ratio += _clock.dayRatio;
                } else {
                    ratio = 0;
                }

                ratio = Mathf.Clamp(ratio / periodDuration, 0, 1);
            } else {
                ratio = 1;
            }
        } else {
            // Night
            if(startRatio < 0 && _clock.nightRatio > (1 + startRatio)) {
                ratio += Mathf.Clamp((_clock.nightRatio - (1 + startRatio)) / periodDuration, 0, 1);
            } else {
                ratio = 0;
            }
        }

        return ratio;
    }
}
