#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

/// <summary>
/// Development-only lifetime and hold-release tracing for ParticleSingle.
/// </summary>
public static class ParticleSingleLifetimeDebug
{
    private static readonly string[] TraceEffectTokens =
    {
        "wh_heal",
        "wh_might",
        "wh_teleport",
        "it_teleport",
        "wind_strike",
        "el_wind_strike",
        "it_healing_potion",
        "e_u056_a"
    };

    public static bool ShouldTrace(string groupName, L2Particle owner, Transform transform)
    {
        for (int tokenIndex = 0; tokenIndex < TraceEffectTokens.Length; tokenIndex++)
        {
            string token = TraceEffectTokens[tokenIndex];
            if (string.IsNullOrEmpty(token))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(groupName) &&
                groupName.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (owner != null && !string.IsNullOrEmpty(owner.name) &&
                owner.name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            Transform t = transform;
            for (int depth = 0; t != null && depth < 16; depth++, t = t.parent)
            {
                if (!string.IsNullOrEmpty(t.name) &&
                    t.name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool ShouldTraceGroupName(string groupName)
    {
        if (string.IsNullOrEmpty(groupName))
        {
            return false;
        }

        for (int tokenIndex = 0; tokenIndex < TraceEffectTokens.Length; tokenIndex++)
        {
            string token = TraceEffectTokens[tokenIndex];
            if (!string.IsNullOrEmpty(token) &&
                groupName.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    public static void LogPlay(in ParticleSingleDebugSnapshot snap)
    {
        if (!ShouldTrace(snap.GroupName, snap.Owner, snap.Transform))
        {
            return;
        }

        Debug.Log(
            $"[PARTICLE_SINGLE_PLAY] group='{snap.GroupName}' playAt={snap.LastEnable:F3}s " +
            $"duration={snap.Duration:F3}s baseShaderLife={snap.BaseShaderLifetime:F3}s fixedDuration={snap.HasFixedDuration} " +
            $"runtimeLoop={snap.RuntimeContinuousLoop} loopOv={snap.HasLoopOverride}:{snap.LoopOverrideValue} " +
            $"maxCount={snap.MaxCount} cps={snap.CountPerSecond} startDelay={snap.StartDelay:F3}s preserveShaderTime={snap.PreserveShaderTime}.");
    }

    public static void LogSetup(in ParticleSingleDebugSnapshot snap, float durationBefore)
    {
        float castHit = snap.CastData != null ? snap.CastData.HitTime : 0f;
        float settingsLife = snap.Settings != null ? snap.Settings.defaultLifeTime : 0f;
        bool serverOverrides = castHit > 0f && settingsLife > castHit + 0.01f;
        EffectCastDurationResolver.LogMismatchIfNeeded(
            "ParticleSingle.Setup",
            snap.GroupName,
            castHit,
            settingsLife,
            snap.Duration,
            serverOverrides);

        if (snap.Settings != null && snap.Settings.hideTime <= 1e-4f)
        {
            Debug.LogWarning(
                $"[PARTICLE_SINGLE_HIDE_TIME_ZERO] group='{snap.GroupName}' hideTime=0 — " +
                "BeginFadeOut и Destroy в один кадр; задайте customHideTime на композите (0.5–1.0с).");
        }

        if (!ShouldTrace(snap.GroupName, snap.Owner, snap.Transform))
        {
            return;
        }

        Debug.Log(
            $"[PARTICLE_SINGLE_SETUP] group='{snap.GroupName}' owner='{(snap.Owner != null ? snap.Owner.name : "null")}' " +
            $"durationBefore={durationBefore:F3}s durationAfter={snap.Duration:F3}s fixedDuration={snap.HasFixedDuration} " +
            $"castHit={castHit:F3}s settingsLife={settingsLife:F3}s legacyHit={snap.LegacyHitDuration:F3}s " +
            $"settingsHide={(snap.Settings != null ? snap.Settings.hideTime : -1f):F3}s preserveShaderTime={snap.PreserveShaderTime}.");
    }

    public static void LogStopPart(in ParticleSingleDebugSnapshot snap, float now, Renderer renderer, Material stopMat)
    {
        if (!ShouldTrace(snap.GroupName, snap.Owner, snap.Transform))
        {
            return;
        }

        Debug.Log(
            $"[PARTICLE_SINGLE_STOPPART] group='{snap.GroupName}' now={now:F3}s elapsed={(now - snap.LastEnable):F3}s " +
            $"duration={snap.Duration:F3}s castHit={(snap.CastData != null ? snap.CastData.HitTime : -1f):F3}s " +
            $"settingsLife={(snap.Settings != null ? snap.Settings.defaultLifeTime : -1f):F3}s " +
            $"[FADE_PHASE]={BuildFirstMaterialFadePhase(renderer, now)} mats=[{BuildAllRuntimeMaterialsFadeDiag(renderer, now)}] " +
            $"vis={ShaderFadeDiagnostic.BuildRendererVisibilityLine(renderer, stopMat, now)}.");
    }

    public static void LogHoldReleaseDefer(string groupName, in L2ShaderHoldController.CastTimeline timeline, in L2ShaderHoldController.Settings settings)
    {
        Debug.Log(
            $"[SHADER_HOLD_RELEASE_DEFER] group='{groupName}' now={timeline.Now:F3}s castElapsed={L2ShaderHoldController.ResolveCastElapsed(in timeline):F4}s " +
            $"castDur={L2ShaderHoldController.ResolveCastDuration(in timeline):F3}s hideWindow={L2ShaderHoldController.ResolveCastEndFadeWindow(in timeline):F3}s " +
            $"hold={settings.ShaderHold:F3} releaseStart={settings.ReleaseStartNormalized:F3} " +
            "reason=BeginFadeOut/StopPart deferred until hold release + cast-end fade finish.");
    }

    public static void LogHoldReleaseDone(string groupName, in L2ShaderHoldController.CastTimeline timeline)
    {
        Debug.Log(
            $"[SHADER_HOLD_RELEASE_DONE] group='{groupName}' now={timeline.Now:F3}s castElapsed={L2ShaderHoldController.ResolveCastElapsed(in timeline):F4}s " +
            $"castDur={L2ShaderHoldController.ResolveCastDuration(in timeline):F3}s hold=0 emitterFade=0.");
    }

    public static void LogSlotOff(in ParticleSingleDebugSnapshot snap, float now)
    {
        if (!ShouldTrace(snap.GroupName, snap.Owner, snap.Transform))
        {
            return;
        }

        Renderer renderer = snap.Renderer;
        Debug.Log(
            $"[PARTICLE_SINGLE_SLOT_OFF] group='{snap.GroupName}' now={now:F3}s alive={(now - snap.SpawnedAt):F3}s " +
            $"duration={snap.Duration:F3}s preserveShaderTime={snap.PreserveShaderTime} " +
            $"forceSpawnLoop={snap.ForceContinuousSpawning} runtimeLoop={snap.RuntimeContinuousLoop} loopOv={snap.HasLoopOverride}:{snap.LoopOverrideValue} " +
            $"mats=[{BuildAllRuntimeMaterialsFadeDiag(renderer, now)}].");
    }

    public static void LogStopMax(in ParticleSingleDebugSnapshot snap, float now)
    {
        if (!ShouldTrace(snap.GroupName, snap.Owner, snap.Transform))
        {
            return;
        }

        Debug.Log(
            $"[PARTICLE_SINGLE_STOP_MAX] group='{snap.GroupName}' now={now:F3}s spawned={snap.SpawnedCount}/{snap.MaxCount} " +
            $"duration={snap.Duration:F3}s shouldLoop={snap.ShouldLoopContinuously} " +
            $"mats=[{BuildAllRuntimeMaterialsFadeDiag(snap.Renderer, now)}].");
    }

    public static void LogCheck500ms(in ParticleSingleDebugSnapshot snap, float now, Renderer renderer, Material checkMat)
    {
        if (!ShouldTrace(snap.GroupName, snap.Owner, snap.Transform))
        {
            return;
        }

        Debug.Log(
            $"[PARTICLE_SINGLE_CHECK_500MS] group='{snap.GroupName}' now={now:F3}s alive={(now - snap.SpawnedAt):F3}s " +
            $"duration={snap.Duration:F3}s baseShaderLife={snap.BaseShaderLifetime:F3}s fixedDuration={snap.HasFixedDuration} " +
            $"preserveShaderTime={snap.PreserveShaderTime} runtimeLoop={snap.RuntimeContinuousLoop} " +
            $"loopOv={snap.HasLoopOverride}:{snap.LoopOverrideValue} " +
            $"rendererActive={(renderer != null && renderer.gameObject.activeSelf)} " +
            $"[FADE_PHASE]={BuildFirstMaterialFadePhase(renderer, now)} mats=[{BuildAllRuntimeMaterialsFadeDiag(renderer, now)}] " +
            $"vis={ShaderFadeDiagnostic.BuildRendererVisibilityLine(renderer, checkMat, now)}.");
    }

    public static void LogTick(in ParticleSingleDebugSnapshot snap, float now)
    {
        if (!ShouldTrace(snap.GroupName, snap.Owner, snap.Transform))
        {
            return;
        }

        Debug.Log(
            $"[PARTICLE_SINGLE_TICK] group='{snap.GroupName}' now={now:F3}s alive={(now - snap.SpawnedAt):F3}s " +
            $"duration={snap.Duration:F3}s castHit={(snap.CastData != null ? snap.CastData.HitTime : -1f):F3}s " +
            $"settingsLife={(snap.Settings != null ? snap.Settings.defaultLifeTime : -1f):F3}s " +
            $"activeLoop={snap.ShouldLoopContinuously} " +
            $"[FADE_PHASE]={BuildFirstMaterialFadePhase(snap.Renderer, now)} mats=[{BuildAllRuntimeMaterialsFadeDiag(snap.Renderer, now)}].");
    }

    public static void LogSpawn(
        in ParticleSingleDebugSnapshot snap,
        int matIndex,
        float now,
        float shaderStartTime,
        float relativeWarmup,
        float seed,
        float holdValue,
        bool releaseByCast,
        float releaseStart,
        bool smoothRelease,
        Renderer renderer,
        Material runtimeMat)
    {
        if (!ShouldTrace(snap.GroupName, snap.Owner, snap.Transform))
        {
            return;
        }

        Debug.Log(
            $"[PARTICLE_SINGLE_SPAWN] group='{snap.GroupName}' idx={matIndex} now={now:F3}s shaderStartTime={shaderStartTime:F3}s relativeWarmup={relativeWarmup:F3}s _Seed={seed:F3} " +
            $"castHit={(snap.CastData != null ? snap.CastData.HitTime : -1f):F3}s settingsLife={(snap.Settings != null ? snap.Settings.defaultLifeTime : -1f):F3}s " +
            $"duration={snap.Duration:F3}s runtimeLoop={snap.RuntimeContinuousLoop} preserveShaderTime={snap.PreserveShaderTime} " +
            $"shaderHold={holdValue:F3} releaseByCast={releaseByCast} releaseStart={releaseStart:F3} smoothRelease={smoothRelease} " +
            $"[FADE_PHASE]={ShaderFadeDiagnostic.FadePhaseLabel(runtimeMat, now)} " +
            $"diag={ShaderFadeDiagnostic.BuildLine(runtimeMat, now)} " +
            $"vis={ShaderFadeDiagnostic.BuildRendererVisibilityLine(renderer, runtimeMat, now)}");
    }

    public static void LogSetActive(in ParticleSingleDebugSnapshot snap, bool value, string reason, Renderer renderer, bool wasActive)
    {
        if (!ShouldTrace(snap.GroupName, snap.Owner, snap.Transform))
        {
            return;
        }

        if (renderer == null && !value)
        {
            Debug.Log(
                $"[PARTICLE_SINGLE_SETACTIVE] group='{snap.GroupName}' value={value} reason='{reason}' renderer=null.");
            return;
        }

        if (wasActive == value)
        {
            return;
        }

        float now = snap.Now;
        Debug.Log(
            $"[PARTICLE_SINGLE_SETACTIVE] group='{snap.GroupName}' value={value} reason='{reason}' " +
            $"now={now:F3}s spawnedAt={snap.SpawnedAt:F3}s alive={(snap.SpawnedAt > 0f ? now - snap.SpawnedAt : -1f):F3}s " +
            $"duration={snap.Duration:F3}s preserveShaderTime={snap.PreserveShaderTime} " +
            $"active={snap.Active} stopped={snap.Stopped} renderer='{renderer.name}'.");
    }

    public static void LogLifetimeFallback(string groupName, L2Particle owner, Transform transform, float fallback, string reason, string matName = null, int matIndex = -1, float lifetime = -1f)
    {
        if (!ShouldTrace(groupName, owner, transform))
        {
            return;
        }

        if (matName != null)
        {
            Debug.Log(
                $"[PARTICLE_SINGLE_LIFETIME] group='{groupName}' mat='{matName}' idx={matIndex} lifetime={lifetime:F3}s from _LifetimeRange.y.");
            return;
        }

        Debug.Log(
            $"[PARTICLE_SINGLE_LIFETIME] group='{groupName}' fallback={fallback:F3}s reason={reason}.");
    }

    public static void TryLogHoldReleaseTransition(
        string groupName,
        in L2ShaderHoldController.Settings settings,
        in L2ShaderHoldController.CastTimeline timeline,
        float holdValue,
        Material mat,
        float spawnedAt,
        float baseShaderLifetime,
        float slotDuration,
        bool hasLoopOverride,
        bool loopOverrideValue,
        bool castEndFadeRequested,
        ref bool loggedReleaseStart,
        ref bool loggedReleaseResume)
    {
        if (!ShouldTraceGroupName(groupName) && !castEndFadeRequested)
        {
            return;
        }

        if (!hasLoopOverride && !castEndFadeRequested)
        {
            return;
        }

        if (!loopOverrideValue && !castEndFadeRequested)
        {
            return;
        }

        if (settings.ShaderHold <= 1e-4f || !settings.ReleaseByCastProgress)
        {
            return;
        }

        float elapsed = L2ShaderHoldController.ResolveCastElapsed(in timeline);
        float dur = L2ShaderHoldController.ResolveCastDuration(in timeline);
        float castU = Mathf.Clamp01(elapsed / dur);
        float releaseStart = Mathf.Clamp(settings.ReleaseStartNormalized, 0f, 0.999f);
        bool pastReleaseStart = castU >= releaseStart - 1e-4f;
        bool inHoldLoop = holdValue >= settings.ShaderHold - 0.001f && castU < releaseStart - 1e-4f;
        bool holdFullyReleased = holdValue <= 1e-4f;
        float fadeWindow = L2ShaderHoldController.ResolveCastEndFadeWindow(in timeline);
        float emitterFadeMul = elapsed <= dur - fadeWindow
            ? 1f
            : 1f - Mathf.Clamp01((elapsed - (dur - fadeWindow)) / Mathf.Max(1e-4f, fadeWindow));

        BuildHoldReleaseAgeSnapshot(
            mat,
            timeline.Now,
            holdValue,
            settings.ShaderHold,
            spawnedAt,
            baseShaderLifetime,
            out float shaderAge,
            out float motionAge,
            out float spinAge,
            out float loopAgeNorm,
            out float sizeAgeNorm,
            out float lifeMax);
        string ageLine = BuildHoldReleaseAgeLine(shaderAge, motionAge, spinAge, loopAgeNorm, sizeAgeNorm, lifeMax);

        if (!loggedReleaseStart && pastReleaseStart && !inHoldLoop)
        {
            loggedReleaseStart = true;
            Debug.Log(
                $"[SHADER_HOLD_RELEASE_START] group='{groupName}' now={timeline.Now:F3}s castElapsed={elapsed:F4}s castU={castU:F4} " +
                $"hold={holdValue:F4} targetHold={settings.ShaderHold:F3} releaseStart={releaseStart:F3} smooth={settings.SmoothRelease} " +
                $"castDur={dur:F3}s slotDur={slotDuration:F3}s settingsLife={(timeline.Settings != null ? timeline.Settings.defaultLifeTime : -1f):F3}s {ageLine} " +
                "effect=hold loop ending, motion/fade/lifetime begin unwinding from hold cap.");
        }

        if (!loggedReleaseResume && holdFullyReleased && (loggedReleaseStart || pastReleaseStart))
        {
            LogHoldReleaseResume(
                groupName,
                in settings,
                in timeline,
                elapsed,
                castU,
                holdValue,
                releaseStart,
                dur,
                fadeWindow,
                emitterFadeMul,
                ageLine,
                ref loggedReleaseResume);
        }
    }

    public static void EnsureHoldReleaseResumeLogged(
        string groupName,
        in L2ShaderHoldController.Settings settings,
        in L2ShaderHoldController.CastTimeline timeline,
        float holdValue,
        float emitterFadeMul,
        Material[] materials,
        float spawnedAt,
        float baseShaderLifetime,
        float slotDuration,
        ref bool loggedReleaseResume)
    {
        if (loggedReleaseResume)
        {
            return;
        }

        Material mat = materials != null && materials.Length > 0 ? materials[0] : null;
        float elapsed = L2ShaderHoldController.ResolveCastElapsed(in timeline);
        float dur = L2ShaderHoldController.ResolveCastDuration(in timeline);
        float castU = Mathf.Clamp01(elapsed / dur);
        float releaseStart = Mathf.Clamp(settings.ReleaseStartNormalized, 0f, 0.999f);
        float fadeWindow = L2ShaderHoldController.ResolveCastEndFadeWindow(in timeline);

        BuildHoldReleaseAgeSnapshot(
            mat,
            timeline.Now,
            holdValue,
            settings.ShaderHold,
            spawnedAt,
            baseShaderLifetime,
            out float shaderAge,
            out float motionAge,
            out float spinAge,
            out float loopAgeNorm,
            out float sizeAgeNorm,
            out float lifeMax);
        string ageLine = BuildHoldReleaseAgeLine(shaderAge, motionAge, spinAge, loopAgeNorm, sizeAgeNorm, lifeMax);

        LogHoldReleaseResume(
            groupName,
            in settings,
            in timeline,
            elapsed,
            castU,
            holdValue,
            releaseStart,
            dur,
            fadeWindow,
            emitterFadeMul,
            ageLine,
            ref loggedReleaseResume);
    }

    private static void LogHoldReleaseResume(
        string groupName,
        in L2ShaderHoldController.Settings settings,
        in L2ShaderHoldController.CastTimeline timeline,
        float elapsed,
        float castU,
        float holdValue,
        float releaseStart,
        float dur,
        float fadeWindow,
        float emitterFadeMul,
        string ageLine,
        ref bool loggedReleaseResume)
    {
        loggedReleaseResume = true;
        Debug.Log(
            $"[SHADER_HOLD_RELEASE_RESUME] group='{groupName}' now={timeline.Now:F3}s castElapsed={elapsed:F4}s castU={castU:F4} " +
            $"hold={holdValue:F4} releaseStart={releaseStart:F3} smooth={settings.SmoothRelease} castDur={dur:F3}s " +
            $"emitterFadeMul={emitterFadeMul:F3} hideWindow={fadeWindow:F3}s {ageLine} " +
            "effect=resumed from hold loop — motionAge follows shaderAge, cast-end emitter fade active.");
    }

    private static void BuildHoldReleaseAgeSnapshot(
        Material mat,
        float now,
        float hold,
        float shaderHold,
        float spawnedAt,
        float baseShaderLifetime,
        out float shaderAge,
        out float motionAge,
        out float spinAge,
        out float loopAgeNorm,
        out float sizeAgeNorm,
        out float lifeMax)
    {
        lifeMax = Mathf.Max(1e-4f, baseShaderLifetime);
        if (mat != null && mat.HasProperty(L2MaterialPropertyCopier.LifetimeRangeId))
        {
            Vector4 life = mat.GetVector(L2MaterialPropertyCopier.LifetimeRangeId);
            lifeMax = Mathf.Max(life.x, life.y, 1e-4f);
        }

        shaderAge = -1f;
        if (mat != null && mat.HasProperty(L2MaterialPropertyCopier.StartTimeId))
        {
            float startTime = mat.GetFloat(L2MaterialPropertyCopier.StartTimeId);
            if (startTime > -0.5f)
            {
                shaderAge = Mathf.Max(0f, now - startTime);
            }
        }

        if (shaderAge < 0f)
        {
            shaderAge = Mathf.Max(0f, now - spawnedAt);
        }

        spinAge = shaderAge;
        float holdSizeReference = shaderHold;
        if (mat != null && mat.HasProperty(L2MaterialPropertyCopier.HoldSizeReferenceId))
        {
            holdSizeReference = mat.GetFloat(L2MaterialPropertyCopier.HoldSizeReferenceId);
        }

        float linearNorm = Mathf.Clamp01(shaderAge / lifeMax);
        float releaseT = 0f;
        if (holdSizeReference > 1e-4f)
        {
            if (hold <= 1e-4f)
            {
                releaseT = 1f;
            }
            else if (hold < holdSizeReference - 1e-4f)
            {
                releaseT = 1f - hold / holdSizeReference;
            }
        }

        float capSec = lifeMax * holdSizeReference;
        if (holdSizeReference <= 1e-4f)
        {
            motionAge = shaderAge;
            sizeAgeNorm = linearNorm;
        }
        else if (shaderAge < capSec)
        {
            motionAge = shaderAge;
            sizeAgeNorm = linearNorm;
        }
        else if (releaseT <= 1e-4f)
        {
            motionAge = capSec;
            sizeAgeNorm = Mathf.Clamp01(holdSizeReference);
        }
        else
        {
            motionAge = Mathf.Lerp(capSec, shaderAge, releaseT);
            sizeAgeNorm = Mathf.Lerp(holdSizeReference, 1f, releaseT);
        }

        if (hold <= 1e-4f)
        {
            loopAgeNorm = linearNorm;
            return;
        }

        float holdNorm = Mathf.Clamp01(hold);
        float holdSec = lifeMax * holdNorm;
        if (shaderAge <= holdSec)
        {
            loopAgeNorm = linearNorm;
            return;
        }

        float tailDuration = Mathf.Max(1e-4f, lifeMax - holdSec);
        float tailPhase = (shaderAge - holdSec) % tailDuration / tailDuration;
        loopAgeNorm = Mathf.Clamp01(holdNorm + tailPhase * (1f - holdNorm));
    }

    private static string BuildHoldReleaseAgeLine(
        float shaderAge,
        float motionAge,
        float spinAge,
        float loopAgeNorm,
        float sizeAgeNorm,
        float lifeMax)
    {
        return
            $"shaderAge={shaderAge:F4}s motionAge={motionAge:F4}s spinAge={spinAge:F4}s " +
            $"loopAgeNorm={loopAgeNorm:F4} sizeAgeNorm={sizeAgeNorm:F4} lifeMax={lifeMax:F4}s";
    }

    private static string BuildFirstMaterialFadePhase(Renderer renderer, float now)
    {
        if (renderer == null)
        {
            return "no_renderer";
        }

        Material[] mats = renderer.materials;
        if (mats == null || mats.Length == 0 || mats[0] == null)
        {
            return "no_mat";
        }

        return ShaderFadeDiagnostic.FadePhaseLabel(mats[0], now);
    }

    private static string BuildAllRuntimeMaterialsFadeDiag(Renderer renderer, float now)
    {
        if (renderer == null)
        {
            return "no_renderer";
        }

        Material[] mats = renderer.materials;
        if (mats == null || mats.Length == 0)
        {
            return "no_runtime_material";
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < mats.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(" | ");
            }

            sb.Append('[').Append(i).Append("] ").Append(ShaderFadeDiagnostic.BuildLine(mats[i], now));
        }

        return sb.ToString();
    }

    private static readonly System.Collections.Generic.Dictionary<string, float> CastEndFadeAlphaLastLogAt =
        new System.Collections.Generic.Dictionary<string, float>();

    /// <summary>
    /// Tracks MeshEmitter1 transparency during cast-end _EmitterAlpha fade (always in Editor/Dev builds).
    /// Filter console: MESH_EMITTER1_FADE_ALPHA
    /// </summary>
    public static void LogCastEndFadeAlpha(
        string groupName,
        in L2ShaderHoldController.CastTimeline timeline,
        Material mat,
        float holdValue,
        float baseEmitterAlpha,
        float fadeMul,
        float holdReleaseMul)
    {
        if (string.IsNullOrEmpty(groupName)
            || groupName.IndexOf("MeshEmitter1", System.StringComparison.OrdinalIgnoreCase) < 0)
        {
            return;
        }

        float now = timeline.Now;
        if (CastEndFadeAlphaLastLogAt.TryGetValue(groupName, out float lastAt) && now - lastAt < 0.1f)
        {
            return;
        }

        CastEndFadeAlphaLastLogAt[groupName] = now;

        float elapsed = L2ShaderHoldController.ResolveCastElapsed(in timeline);
        float castDur = L2ShaderHoldController.ResolveCastDuration(in timeline);
        float hideWindow = L2ShaderHoldController.ResolveCastEndFadeWindow(in timeline);
        float emitterAlpha = mat != null && mat.HasProperty(L2MaterialPropertyCopier.EmitterAlphaId)
            ? mat.GetFloat(L2MaterialPropertyCopier.EmitterAlphaId)
            : -1f;
        float opacity = mat != null && mat.HasProperty(L2MaterialPropertyCopier.OpacityId)
            ? mat.GetFloat(L2MaterialPropertyCopier.OpacityId)
            : -1f;
        float lifeAlpha = EstimateShaderLifeAlpha(mat, now);
        float approxFramebufferAlpha = opacity >= 0f && emitterAlpha >= 0f && lifeAlpha >= 0f
            ? opacity * emitterAlpha * lifeAlpha
            : -1f;

        Debug.Log(
            $"[MESH_EMITTER1_FADE_ALPHA] group='{groupName}' now={now:F3}s " +
            $"castElapsed={elapsed:F3}s castDur={castDur:F3}s hideWindow={hideWindow:F3}s " +
            $"fadeMul={fadeMul:F4} holdReleaseMul={holdReleaseMul:F4} baseEmitterA={baseEmitterAlpha:F4} matEmitterA={emitterAlpha:F4} " +
            $"opacity={opacity:F4} lifeAlpha~={lifeAlpha:F4} approxOutAlpha~={approxFramebufferAlpha:F4} " +
            $"hold={holdValue:F4} sizeAgeNorm~={EstimateShaderSizeAgeNorm(mat, now):F4} " +
            $"phase={ShaderFadeDiagnostic.FadePhaseLabel(mat, now)} " +
            $"diag={ShaderFadeDiagnostic.BuildLine(mat, now)}");
    }

    private static float EstimateShaderLifeAlpha(Material mat, float now)
    {
        if (mat == null)
        {
            return -1f;
        }

        float hasLt = mat.HasProperty(L2MaterialPropertyCopier.HasLifetimeId)
            ? mat.GetFloat(L2MaterialPropertyCopier.HasLifetimeId)
            : 1f;
        float hold = mat.HasProperty(L2MaterialPropertyCopier.HoldId)
            ? mat.GetFloat(L2MaterialPropertyCopier.HoldId)
            : 0f;
        float fadeIn = mat.HasProperty(L2MaterialPropertyCopier.FadeInId)
            ? mat.GetFloat(L2MaterialPropertyCopier.FadeInId)
            : 0f;
        float fadeInEnd = mat.HasProperty(L2MaterialPropertyCopier.FadeInEndTimeId)
            ? mat.GetFloat(L2MaterialPropertyCopier.FadeInEndTimeId)
            : 0f;
        float fadeOut = mat.HasProperty(L2MaterialPropertyCopier.FadeoutId)
            ? mat.GetFloat(L2MaterialPropertyCopier.FadeoutId)
            : 0f;
        float fadeOutStart = mat.HasProperty(L2MaterialPropertyCopier.FadeoutStartTimeId)
            ? mat.GetFloat(L2MaterialPropertyCopier.FadeoutStartTimeId)
            : float.MaxValue;
        Vector4 life = mat.HasProperty(L2MaterialPropertyCopier.LifetimeRangeId)
            ? mat.GetVector(L2MaterialPropertyCopier.LifetimeRangeId)
            : Vector4.one;
        float lifeMax = Mathf.Max(life.x, life.y, 1e-4f);
        float st = mat.HasProperty(L2MaterialPropertyCopier.StartTimeId)
            ? mat.GetFloat(L2MaterialPropertyCopier.StartTimeId)
            : -1f;
        if (st < -0.49f)
        {
            return -1f;
        }

        float age = Mathf.Max(0f, now - st);
        float fadeAge = hold > 1e-4f ? Mathf.Min(age, lifeMax * hold) : age;
        if (hasLt < 0.5f)
        {
            fadeAge = Mathf.Repeat(age, lifeMax);
        }

        float fadeInMul = fadeIn >= 0.5f
            ? Mathf.Clamp01(age / Mathf.Max(1e-4f, fadeInEnd))
            : 1f;
        float fadeOutMul = 1f;
        if (fadeOut >= 0.5f && hold <= 1e-4f)
        {
            float fadeStart = Mathf.Clamp(fadeOutStart, 0f, lifeMax);
            float fadeDuration = Mathf.Max(1e-4f, lifeMax - fadeStart);
            fadeOutMul = 1f - Mathf.Clamp01((fadeAge - fadeStart) / fadeDuration);
        }

        float lifeAlpha = Mathf.Clamp01(fadeInMul * fadeOutMul);
        if (mat.HasProperty(L2MaterialPropertyCopier.FadeOutPowerId))
        {
            float fadeOutPower = mat.GetFloat(L2MaterialPropertyCopier.FadeOutPowerId);
            if (fadeOutPower > 1.0001f)
            {
                lifeAlpha = Mathf.Pow(lifeAlpha, fadeOutPower);
            }
        }

        return lifeAlpha;
    }

    private static float EstimateShaderSizeAgeNorm(Material mat, float now)
    {
        if (mat == null)
        {
            return -1f;
        }

        float hold = mat.HasProperty(L2MaterialPropertyCopier.HoldId)
            ? mat.GetFloat(L2MaterialPropertyCopier.HoldId)
            : 0f;
        float holdSizeReference = mat.HasProperty(L2MaterialPropertyCopier.HoldSizeReferenceId)
            ? mat.GetFloat(L2MaterialPropertyCopier.HoldSizeReferenceId)
            : 0f;
        Vector4 life = mat.HasProperty(L2MaterialPropertyCopier.LifetimeRangeId)
            ? mat.GetVector(L2MaterialPropertyCopier.LifetimeRangeId)
            : Vector4.one;
        float lifeMax = Mathf.Max(life.x, life.y, 1e-4f);
        float st = mat.HasProperty(L2MaterialPropertyCopier.StartTimeId)
            ? mat.GetFloat(L2MaterialPropertyCopier.StartTimeId)
            : -1f;
        if (st < -0.49f)
        {
            return -1f;
        }

        float shaderAge = Mathf.Max(0f, now - st);
        float linearNorm = Mathf.Clamp01(shaderAge / lifeMax);

        if (holdSizeReference <= 1e-4f)
        {
            return linearNorm;
        }

        float releaseT = 0f;
        if (hold <= 1e-4f)
        {
            releaseT = 1f;
        }
        else if (hold < holdSizeReference - 1e-4f)
        {
            releaseT = 1f - hold / holdSizeReference;
        }

        float capSec = lifeMax * holdSizeReference;
        if (shaderAge < capSec)
        {
            return linearNorm;
        }

        if (releaseT <= 1e-4f)
        {
            return Mathf.Clamp01(holdSizeReference);
        }

        return Mathf.Lerp(holdSizeReference, 1f, releaseT);
    }

    public static void ResetCastEndFadeAlphaLog(string groupName)
    {
        if (!string.IsNullOrEmpty(groupName))
        {
            CastEndFadeAlphaLastLogAt.Remove(groupName);
        }
    }
}

public struct ParticleSingleDebugSnapshot
{
    public string GroupName;
    public Transform Transform;
    public L2Particle Owner;
    public Renderer Renderer;
    public float Now;
    public float LastEnable;
    public float SpawnedAt;
    public float Duration;
    public float BaseShaderLifetime;
    public bool HasFixedDuration;
    public bool PreserveShaderTime;
    public bool ForceContinuousSpawning;
    public bool RuntimeContinuousLoop;
    public bool HasLoopOverride;
    public bool LoopOverrideValue;
    public bool Active;
    public bool Stopped;
    public int SpawnedCount;
    public int MaxCount;
    public int CountPerSecond;
    public float StartDelay;
    public bool ShouldLoopContinuously;
    public EffectSettings Settings;
    public MagicCastData CastData;
    public float LegacyHitDuration;
}
#else
public static class ParticleSingleLifetimeDebug
{
    public static bool ShouldTrace(string groupName, L2Particle owner, Transform transform) => false;

    public static void LogPlay(in ParticleSingleDebugSnapshot snap) { }
    public static void LogSetup(in ParticleSingleDebugSnapshot snap, float durationBefore) { }
    public static void LogStopPart(in ParticleSingleDebugSnapshot snap, float now, Renderer renderer, Material stopMat) { }
    public static void LogHoldReleaseDefer(string groupName, in L2ShaderHoldController.CastTimeline timeline, in L2ShaderHoldController.Settings settings) { }
    public static void LogHoldReleaseDone(string groupName, in L2ShaderHoldController.CastTimeline timeline) { }
    public static void LogSlotOff(in ParticleSingleDebugSnapshot snap, float now) { }
    public static void LogStopMax(in ParticleSingleDebugSnapshot snap, float now) { }
    public static void LogCheck500ms(in ParticleSingleDebugSnapshot snap, float now, Renderer renderer, Material checkMat) { }
    public static void LogTick(in ParticleSingleDebugSnapshot snap, float now) { }
    public static void LogSpawn(
        in ParticleSingleDebugSnapshot snap,
        int matIndex,
        float now,
        float shaderStartTime,
        float relativeWarmup,
        float seed,
        float holdValue,
        bool releaseByCast,
        float releaseStart,
        bool smoothRelease,
        Renderer renderer,
        Material runtimeMat) { }
    public static void LogSetActive(in ParticleSingleDebugSnapshot snap, bool value, string reason, Renderer renderer, bool wasActive) { }
    public static void LogLifetimeFallback(string groupName, L2Particle owner, Transform transform, float fallback, string reason, string matName = null, int matIndex = -1, float lifetime = -1f) { }

    public static void TryLogHoldReleaseTransition(
        string groupName,
        in L2ShaderHoldController.Settings settings,
        in L2ShaderHoldController.CastTimeline timeline,
        float holdValue,
        Material mat,
        float spawnedAt,
        float baseShaderLifetime,
        float slotDuration,
        bool hasLoopOverride,
        bool loopOverrideValue,
        bool castEndFadeRequested,
        ref bool loggedReleaseStart,
        ref bool loggedReleaseResume) { }

    public static void EnsureHoldReleaseResumeLogged(
        string groupName,
        in L2ShaderHoldController.Settings settings,
        in L2ShaderHoldController.CastTimeline timeline,
        float holdValue,
        float emitterFadeMul,
        Material[] materials,
        float spawnedAt,
        float baseShaderLifetime,
        float slotDuration,
        ref bool loggedReleaseResume) { }

    public static void LogCastEndFadeAlpha(
        string groupName,
        in L2ShaderHoldController.CastTimeline timeline,
        Material mat,
        float holdValue,
        float baseEmitterAlpha,
        float fadeMul,
        float holdReleaseMul) { }

    public static void ResetCastEndFadeAlphaLog(string groupName) { }
}

public struct ParticleSingleDebugSnapshot
{
    public string GroupName;
    public Transform Transform;
    public L2Particle Owner;
    public Renderer Renderer;
    public float Now;
    public float LastEnable;
    public float SpawnedAt;
    public float Duration;
    public float BaseShaderLifetime;
    public bool HasFixedDuration;
    public bool PreserveShaderTime;
    public bool ForceContinuousSpawning;
    public bool RuntimeContinuousLoop;
    public bool HasLoopOverride;
    public bool LoopOverrideValue;
    public bool Active;
    public bool Stopped;
    public int SpawnedCount;
    public int MaxCount;
    public int CountPerSecond;
    public float StartDelay;
    public bool ShouldLoopContinuously;
    public EffectSettings Settings;
    public MagicCastData CastData;
    public float LegacyHitDuration;
}
#endif
