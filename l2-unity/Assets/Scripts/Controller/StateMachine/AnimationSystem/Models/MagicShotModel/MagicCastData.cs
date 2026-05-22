using UnityEngine;

public class MagicCastData
{
    /// <summary>
    /// <b>false</b> — считать <see cref="AnimatorWallPenaltySeconds"/> по кривой ниже (<c>AdjustedShootWindow = serverTimeToShoot − penalty</c>).
    /// <para>
    /// <b>true</b> (сейчас): кривая wall penalty отключена — при fixed duration тайминг клипа уже стабильный,
    /// используем только фиксированный <see cref="CastShootWindowFixedOffsetSeconds"/>.
    /// Код penalty в <c>else</c> намеренно не удалён, чтобы можно было вернуть без восстановления из git.
    /// </para>
    /// </summary>
    private const bool MagicCastWallPenaltyDisabledForTesting = true;

    /// <summary>
    /// Фиксированный сдвиг окна до выстрела (сек): вычитается из номинального <c>serverTimeToShoot</c>,
    /// как раньше «штраф», но постоянный (~150 мс) — выше <c>timingScale</c>, событие выстрела чуть раньше по реальному времени.
    /// </summary>
    private const float CastShootWindowFixedOffsetSeconds = 0.15f;

    /// <summary>При коротком server HitTime (после клампа) — нижнее значение штрафа, сек (~70 мс).</summary>
    private const float WallPenaltyClampHitMinSeconds = 2f;

    /// <summary>При длинном server HitTime (после клампа) — верх штрафа, сек (~150 мс при старой линейной модели).</summary>
    private const float WallPenaltyClampHitMaxSeconds = 8f;

    private const float WallPenaltySecondsMin = 0.07f;
    private const float WallPenaltySecondsMax = 0.15f;

    public float StartTime;
    public float HitTime;
    public float FlightTime;

    /// <summary>Неучтённые в таймлинке переходы/оверлапы (масштаб от server HitTime), только если penalty включён.</summary>
    public float AnimatorWallPenaltySeconds { get; private set; }

    // Индивидуальные скорости для каждой фазы
    public float SpeedMid = 1.0f;
    public float SpeedEnd = 1.0f;
    public float SpeedShot = 1.0f;
    public float shotEventTime;
    public float serverTimeToShoot;

    /// <summary>Object id цели из пакета MagicSkillUse (приоритетнее PlayerEntity.TargetId для VFX).</summary>
    public int TargetObjectId;

    /// <summary>Окно от старта каста до момента выстрела: номинал минус penalty или минус фиксированный offset.</summary>
    public float AdjustedShootWindowSeconds { get; private set; }

    public void Setup(float serverHitMs, float flyMs, float[] clipsDurations, float shotEventTime, int targetObjectId = 0)
    {
        StartTime = Time.time;
        HitTime = serverHitMs / 1000f;
        FlightTime = flyMs / 1000f;
        TargetObjectId = targetObjectId;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"[MagicCastData] serverHitMs={serverHitMs:F1}ms hitTime={HitTime:F3}s " +
            $"flyMs={flyMs:F1}ms flightTime={FlightTime:F3}s shotEventTime={shotEventTime:F3}s");
#endif

        serverTimeToShoot = Mathf.Max(0.01f, HitTime - FlightTime);

        float penaltyTLinear;
        float penaltyT;
        if (MagicCastWallPenaltyDisabledForTesting)
        {
            AnimatorWallPenaltySeconds = 0f;
            AdjustedShootWindowSeconds = Mathf.Max(
                0.01f,
                serverTimeToShoot - CastShootWindowFixedOffsetSeconds);
            penaltyTLinear = 0f;
            penaltyT = 0f;
        }
        else
        {
            // Wall penalty (кривая по HitTime) — сейчас выключено флагом выше; ветка сохранена для возврата к динамическому штрафу.
            float hitSeconds = HitTime;
            float hitClamped = Mathf.Clamp(
                hitSeconds,
                WallPenaltyClampHitMinSeconds,
                WallPenaltyClampHitMaxSeconds);
            penaltyTLinear = Mathf.InverseLerp(
                WallPenaltyClampHitMinSeconds,
                WallPenaltyClampHitMaxSeconds,
                hitClamped);
            penaltyT = penaltyTLinear;
            AnimatorWallPenaltySeconds = Mathf.Lerp(WallPenaltySecondsMin, WallPenaltySecondsMax, penaltyTLinear);

            AdjustedShootWindowSeconds = Mathf.Max(
                0.01f,
                serverTimeToShoot - AnimatorWallPenaltySeconds);
        }

        float durMid = clipsDurations[0];
        float durEnd = clipsDurations[1];
        float durShotToEvent = (shotEventTime > 0f) ? shotEventTime : clipsDurations[2];
        this.shotEventTime = durShotToEvent;

        float timelineDuration = durMid + durEnd + durShotToEvent;
        float timingScale = timelineDuration / AdjustedShootWindowSeconds;
        float timingScaleWithoutWallPenalty = timelineDuration / serverTimeToShoot;

        SpeedMid = timingScale;
        SpeedEnd = timingScale;
        SpeedShot = timingScale;

        string wallPenaltyDisabledLabel = MagicCastWallPenaltyDisabledForTesting ? "YES" : "NO";
        Debug.Log(
            $"[CAST_WALL_PENALTY] DISABLED={wallPenaltyDisabledLabel} offsetMs={(MagicCastWallPenaltyDisabledForTesting ? CastShootWindowFixedOffsetSeconds : 0f) * 1000f:F0} " +
            $"penaltyMs={AnimatorWallPenaltySeconds * 1000f:F2} curveT={penaltyT:F3} tLin={penaltyTLinear:F3} " +
            $"hitSec={HitTime:F3} winNomSec={serverTimeToShoot:F3} winAdjSec={AdjustedShootWindowSeconds:F3} " +
            $"timelineSec={timelineDuration:F3} scaleNoPenalty={timingScaleWithoutWallPenalty:F4} scaleApplied={timingScale:F4} " +
            $"deltaScale={(timingScale - timingScaleWithoutWallPenalty):F4}");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"[CastTimingSetup] hit={HitTime:F3}s flight={FlightTime:F3}s " +
            $"shootTargetNominal={serverTimeToShoot:F3}s wallPenalty={AnimatorWallPenaltySeconds:F3}s " +
            $"(curveT={penaltyT:F3} tLin={penaltyTLinear:F3}) adjustedShootWin={AdjustedShootWindowSeconds:F3}s " +
            $"durMid={durMid:F3}s durEnd={durEnd:F3}s durShotToEvent={durShotToEvent:F3}s shotEventResolved={this.shotEventTime:F3}s " +
            $"timelineDuration={timelineDuration:F3}s timingScale={timingScale:F3} " +
            $"speedMid={SpeedMid:F3} speedEnd={SpeedEnd:F3} speedShot={SpeedShot:F3}");
#endif
    }

    public float GetShotTimeNormalize()
    {
        float fadeStartProgress = serverTimeToShoot / HitTime;
        float shaderFadeStart = Mathf.Max(0, (serverTimeToShoot / HitTime));
        return shaderFadeStart;
    }
}
