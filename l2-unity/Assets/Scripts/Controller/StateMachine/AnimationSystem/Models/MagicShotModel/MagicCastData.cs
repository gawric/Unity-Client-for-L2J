using UnityEngine;

public class MagicCastData
{
    private const float MinPhaseWindowSeconds = 0.01f;

    /// <summary>Эталонное окно до выстрела: ReferenceHitTime − CastShootWindowFixedOffset.</summary>
    private const float ReferenceAdjustedShootWindowSeconds = 1.35f;
    /// <summary>Эталонная пауза в стойке CastEnd2P после клипа (сек) при ReferenceHitTime.</summary>
    private const float ReferenceTwoClipCastEndHoldSeconds = 0.4f;
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

    /// <summary>Wall-clock удержание позы после CastEnd2P (не замедляет сам клип).</summary>
    public float TwoClipCastEndHoldSeconds { get; private set; }

    /// <summary>Long-cast path (scrolls / teleports): no animator speed scaling.</summary>
    public bool IsLongCast;

    public float DurMid;
    public float DurEnd;
    public float DurShot;

    /// <summary>Seconds from cast start when MagicShotLong should fire (HitTime − DurShot).</summary>
    public float ShotTriggerGlobalOffset;

    public void SetupLongCast(float serverHitMs, float[] clipsDurations, float shotEventTime, int targetObjectId = 0)
    {
        IsLongCast = true;
        StartTime = Time.time;
        HitTime = serverHitMs / 1000f;
        FlightTime = 0f;
        TargetObjectId = targetObjectId;
        TwoClipCastEndHoldSeconds = 0f;
        AnimatorWallPenaltySeconds = 0f;

        SpeedMid = 1f;
        SpeedEnd = 1f;
        SpeedShot = 1f;

        DurMid = GetDurationSafe(clipsDurations, 0);
        DurEnd = GetDurationSafe(clipsDurations, 1);
        DurShot = GetDurationSafe(clipsDurations, 2);

        float durShotToEvent = shotEventTime > 0f ? shotEventTime : DurShot;
        this.shotEventTime = durShotToEvent;
        serverTimeToShoot = Mathf.Max(0.01f, HitTime - FlightTime);
        ShotTriggerGlobalOffset = Mathf.Max(0f, HitTime - DurShot);
        AdjustedShootWindowSeconds = ShotTriggerGlobalOffset;

        Debug.Log(
            $"[LongCastData] hit={HitTime:F3}s shotTriggerAt={ShotTriggerGlobalOffset:F3}s " +
            $"clips[mid={DurMid:F3},end={DurEnd:F3},shot={DurShot:F3},shotEvt={durShotToEvent:F3}] " +
            $"loopEndSec≈{Mathf.Max(0f, ShotTriggerGlobalOffset - DurMid):F3}");
    }

    public void Setup(float serverHitMs, float flyMs, float[] clipsDurations, float shotEventTime, int targetObjectId = 0)
    {
        IsLongCast = false;
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

        float durMid = GetDurationSafe(clipsDurations, 0);
        float durEnd = GetDurationSafe(clipsDurations, 1);
        float durShot = GetDurationSafe(clipsDurations, 2);

        bool hasMidPhase = durMid > 0f;
        bool hasEndPhase = durEnd > 0f;
        bool hasShotPhase = durShot > 0f;

        bool isTwoClipCastPath = hasMidPhase && hasEndPhase && !hasShotPhase;
        float durShotToEvent;
        float timelineDuration;

        if (isTwoClipCastPath)
        {
            // Two-clip override path: trigger plan is CastEnd -> MagicShot.
            // First clip fully plays as CastEnd, second clip is MagicShot.
            durShotToEvent = (shotEventTime > 0f) ? shotEventTime : durEnd;
            timelineDuration = durMid + durShotToEvent;
        }
        else
        {
            durShotToEvent = (shotEventTime > 0f) ? shotEventTime : durShot;
            timelineDuration = durMid + durEnd + durShotToEvent;
        }

        this.shotEventTime = durShotToEvent;
        timelineDuration = Mathf.Max(0.01f, timelineDuration);
        float timingScale = timelineDuration / AdjustedShootWindowSeconds;
        float timingScaleWithoutWallPenalty = timelineDuration / serverTimeToShoot;

        SpeedMid = timingScale;
        SpeedEnd = timingScale;
        SpeedShot = timingScale;

        float firstWindow = 0f;
        float secondWindow = 0f;
        float twoClipFirstShare = 0f;
        float twoClipSecondShare = 0f;
        float twoClipAnimPlayBudget = 0f;
        float castEndPhaseTotalSec = 0f;
        float holdPctOfHit = 0f;
        float castEndPlayPctOfHit = 0f;
        float magicShotPlayPctOfHit = 0f;
        if (isTwoClipCastPath)
        {
            // Масштабируем эталон (hit=1.5, hold=0.4, winAdj=1.35) на любое server HitTime.
            float shootWindowScale = AdjustedShootWindowSeconds / ReferenceAdjustedShootWindowSeconds;
            TwoClipCastEndHoldSeconds = ReferenceTwoClipCastEndHoldSeconds * shootWindowScale;

            // 1) hold (стойка после CastEnd2P)  2) animPlayBudget режется по длинам клипов на CastEnd2P + MagicShot2P.
            twoClipAnimPlayBudget = Mathf.Max(
                MinPhaseWindowSeconds * 2f,
                AdjustedShootWindowSeconds - TwoClipCastEndHoldSeconds);

            float twoClipTimeline = durMid + durShotToEvent;
            if (twoClipTimeline > MinPhaseWindowSeconds)
            {
                twoClipFirstShare = durMid / twoClipTimeline;
                twoClipSecondShare = durShotToEvent / twoClipTimeline;
            }
            else
            {
                twoClipFirstShare = 0.5f;
                twoClipSecondShare = 0.5f;
            }

            firstWindow = Mathf.Max(MinPhaseWindowSeconds, twoClipAnimPlayBudget * twoClipFirstShare);
            secondWindow = Mathf.Max(MinPhaseWindowSeconds, twoClipAnimPlayBudget * twoClipSecondShare);

            float speedFirst = durMid / firstWindow;
            float speedSecond = durShotToEvent / secondWindow;

            SpeedMid = 1.0f;
            SpeedEnd = speedFirst;
            SpeedShot = speedSecond;

            if (HitTime > MinPhaseWindowSeconds)
            {
                castEndPhaseTotalSec = firstWindow + TwoClipCastEndHoldSeconds;
                holdPctOfHit = TwoClipCastEndHoldSeconds / HitTime * 100f;
                castEndPlayPctOfHit = firstWindow / HitTime * 100f;
                magicShotPlayPctOfHit = secondWindow / HitTime * 100f;
            }
        }
        else
        {
            TwoClipCastEndHoldSeconds = 0f;
        }

        string wallPenaltyDisabledLabel = MagicCastWallPenaltyDisabledForTesting ? "YES" : "NO";
        Debug.Log(
            $"[CAST_WALL_PENALTY] DISABLED={wallPenaltyDisabledLabel} offsetMs={(MagicCastWallPenaltyDisabledForTesting ? CastShootWindowFixedOffsetSeconds : 0f) * 1000f:F0} " +
            $"penaltyMs={AnimatorWallPenaltySeconds * 1000f:F2} curveT={penaltyT:F3} tLin={penaltyTLinear:F3} " +
            $"hitSec={HitTime:F3} winNomSec={serverTimeToShoot:F3} winAdjSec={AdjustedShootWindowSeconds:F3} " +
            $"timelineSec={timelineDuration:F3} scaleNoPenalty={timingScaleWithoutWallPenalty:F4} scaleApplied={timingScale:F4} " +
            $"deltaScale={(timingScale - timingScaleWithoutWallPenalty):F4} " +
            $"speedMid={SpeedMid:F3} speedEnd={SpeedEnd:F3} speedShot={SpeedShot:F3} " +
            $"clips[mid={durMid:F3},end={durEnd:F3},shot={durShot:F3},shotEvt={durShotToEvent:F3}] " +
            $"windows[first={firstWindow:F3},second={secondWindow:F3},clipShareFirst={twoClipFirstShare:F3},clipShareSecond={twoClipSecondShare:F3}] " +
            $"castEndHoldSec={TwoClipCastEndHoldSeconds:F3} animPlayBudget={twoClipAnimPlayBudget:F3} " +
            $"castEndPhaseTotal={castEndPhaseTotalSec:F3} pctOfHit[hold={holdPctOfHit:F1},castPlay={castEndPlayPctOfHit:F1},shotPlay={magicShotPlayPctOfHit:F1}] " +
            $"path={(isTwoClipCastPath ? "two_clip_castend_magicshot" : "default_three_phase")}");

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

    private static float GetDurationSafe(float[] clipsDurations, int index)
    {
        if (clipsDurations == null || index < 0 || index >= clipsDurations.Length)
        {
            return 0f;
        }

        return Mathf.Max(0f, clipsDurations[index]);
    }
}
