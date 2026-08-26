#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;

/// <summary>
/// Development-only tracing for ParticleGroup slot recycle vs shader FadeOut (wh_teleport upline).
/// Filter: effect token wh_teleport + group name SpriteEmitter2. Console: [UPLINE_GROUP_*].
/// </summary>
public static class ParticleGroupLifetimeDebug
{
    private static readonly int StartTimeShaderId = Shader.PropertyToID("_StartTime");
    private static readonly int InitialDelayRangeShaderId = Shader.PropertyToID("_InitialDelayRange");
    private static readonly int LifetimeRangeShaderId = Shader.PropertyToID("_LifetimeRange");
    private static readonly int DebugAtlasPreviewShaderId = Shader.PropertyToID("_DebugAtlasPreview");
    private static readonly int DebugAtlasPreviewLoopShaderId = Shader.PropertyToID("_DebugAtlasPreviewLoop");

    public static bool ShouldTraceUpline(string groupName, L2Particle owner, Transform transform)
    {
        if (!ParticleSingleLifetimeDebug.ShouldTrace(groupName, owner, transform))
        {
            return false;
        }

        if (string.IsNullOrEmpty(groupName))
        {
            return false;
        }

        return groupName.IndexOf("SpriteEmitter2", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static float EstimateVisibleShaderAge(Material mat, float now)
    {
        if (mat == null || !mat.HasProperty(StartTimeShaderId))
        {
            return -1f;
        }

        float startTime = mat.GetFloat(StartTimeShaderId);
        if (startTime < -0.49f)
        {
            return -1f;
        }

        Vector4 idr = mat.HasProperty(InitialDelayRangeShaderId)
            ? mat.GetVector(InitialDelayRangeShaderId)
            : Vector4.zero;
        float delayMax = Mathf.Max(idr.x, idr.y);
        return Mathf.Max(0f, now - startTime - delayMax);
    }

    /// <summary>Why ParticleGroup turned the slot off vs shader fade state.</summary>
    public static string ClassifySlotKill(Material mat, float now, float groupAliveSeconds, float groupDuration)
    {
        float hasLt = mat != null && mat.HasProperty(L2MaterialPropertyCopier.HasLifetimeId)
            ? mat.GetFloat(L2MaterialPropertyCopier.HasLifetimeId)
            : 1f;
        if (hasLt < 0.5f)
        {
            return "HARD_OFF_SHADER_FADE_DISABLED";
        }

        string phase = ShaderFadeDiagnostic.FadePhaseLabel(mat, now);
        float visibleAge = EstimateVisibleShaderAge(mat, now);
        Vector4 life = mat != null && mat.HasProperty(LifetimeRangeShaderId)
            ? mat.GetVector(LifetimeRangeShaderId)
            : Vector4.zero;
        float lifeMax = Mathf.Max(life.x, life.y, 1e-6f);

        if (visibleAge >= 0f && visibleAge < lifeMax - 0.02f)
        {
            if (phase == "FADE_OUT" || phase == "FADEOUT_START_AFTER_OR_AT_LIFE_MAX")
            {
                return "HARD_OFF_DURING_SHADER_FADE";
            }

            if (phase == "INITIAL_DELAY")
            {
                return "HARD_OFF_DURING_INITIAL_DELAY";
            }

            if (phase == "FULL")
            {
                return "HARD_OFF_BEFORE_SHADER_FADE";
            }
        }

        if (visibleAge >= lifeMax - 0.02f || phase == "SHADER_LIFE_EXPIRED")
        {
            return "HARD_OFF_AFTER_SHADER_LIFE";
        }

        if (groupAliveSeconds + 0.02f < groupDuration)
        {
            return "HARD_OFF_EARLY_VS_GROUP_DURATION";
        }

        return $"HARD_OFF phase={phase}";
    }

    public static void LogPlayPart(
        string groupName,
        L2Particle owner,
        Transform transform,
        float playAt,
        float startDelay,
        float duration,
        float shaderSlotDuration,
        int countPerSecond,
        int maxCount,
        bool preserveShaderTime,
        bool runtimeLoop)
    {
        if (!ShouldTraceUpline(groupName, owner, transform))
        {
            return;
        }

        Debug.Log(
            $"[UPLINE_GROUP_PLAY] group='{groupName}' playAt={playAt:F3}s startDelay={startDelay:F3}s " +
            $"groupDuration={duration:F3}s shaderSlotDuration={shaderSlotDuration:F3}s " +
            $"cps={countPerSecond} maxCount={maxCount} preserveShaderTime={preserveShaderTime} runtimeLoop={runtimeLoop} " +
            $"note=Scene DebugAtlasPreview+Loop skips InitialDelay and slot recycle; Play uses ParticleGroup timing.");
    }

    public static void LogSpawn(
        string groupName,
        L2Particle owner,
        Transform transform,
        int slot,
        float now,
        float shaderStartTime,
        float seed,
        float groupDuration,
        int spawnedCount,
        int maxCount,
        Renderer renderer)
    {
        if (!ShouldTraceUpline(groupName, owner, transform))
        {
            return;
        }

        Material mat = renderer != null && renderer.materials != null && renderer.materials.Length > 0
            ? renderer.materials[0]
            : null;
        float preview = mat != null && mat.HasProperty(DebugAtlasPreviewShaderId)
            ? mat.GetFloat(DebugAtlasPreviewShaderId)
            : -1f;
        float previewLoop = mat != null && mat.HasProperty(DebugAtlasPreviewLoopShaderId)
            ? mat.GetFloat(DebugAtlasPreviewLoopShaderId)
            : -1f;

        Debug.Log(
            $"[UPLINE_GROUP_SPAWN] group='{groupName}' slot={slot} renderer='{(renderer != null ? renderer.name : "null")}' " +
            $"now={now:F3}s shaderStart={shaderStartTime:F3}s seed={seed:F3} spawned={spawnedCount}/{maxCount} " +
            $"groupDuration={groupDuration:F3}s visibleAge~={EstimateVisibleShaderAge(mat, now):F3}s " +
            $"debugAtlasPreview={preview:F0} debugLoop={previewLoop:F0} " +
            $"[FADE_PHASE]={ShaderFadeDiagnostic.FadePhaseLabel(mat, now)} " +
            $"{ShaderFadeDiagnostic.BuildLine(mat, now)} frame={Time.frameCount}.");
    }

    public static void LogSlotOff(
        string groupName,
        L2Particle owner,
        Transform transform,
        int slot,
        float now,
        float spawnedAt,
        float groupDuration,
        Renderer renderer,
        string killReason)
    {
        if (!ShouldTraceUpline(groupName, owner, transform))
        {
            return;
        }

        Material mat = renderer != null && renderer.materials != null && renderer.materials.Length > 0
            ? renderer.materials[0]
            : null;
        float alive = spawnedAt > 0f ? now - spawnedAt : -1f;
        string killClass = ClassifySlotKill(mat, now, alive, groupDuration);

        Debug.Log(
            $"[UPLINE_GROUP_SLOT_OFF] group='{groupName}' slot={slot} renderer='{(renderer != null ? renderer.name : "null")}' " +
            $"now={now:F3}s groupAlive={alive:F3}s groupDuration={groupDuration:F3}s killReason={killReason} " +
            $"killClass={killClass} visibleAge~={EstimateVisibleShaderAge(mat, now):F3}s " +
            $"[FADE_PHASE]={ShaderFadeDiagnostic.FadePhaseLabel(mat, now)} " +
            $"{ShaderFadeDiagnostic.BuildLine(mat, now)} " +
            $"vis={ShaderFadeDiagnostic.BuildRendererVisibilityLine(renderer, mat, now)} frame={Time.frameCount}.");
    }

    public static void LogRespawnWarning(
        string groupName,
        L2Particle owner,
        Transform transform,
        int slot,
        float now,
        float prevSpawnedAt,
        float prevStartTime)
    {
        if (!ShouldTraceUpline(groupName, owner, transform))
        {
            return;
        }

        Debug.LogWarning(
            $"[UPLINE_GROUP_RESPAWN] group='{groupName}' slot={slot} now={now:F3}s " +
            $"prevAlive={(now - prevSpawnedAt):F3}s prevStartTime={prevStartTime:F3}s " +
            $"slot was still active — resetting _StartTime (machine-gun respawn). frame={Time.frameCount}.");
    }

    public static void LogTick(
        string groupName,
        L2Particle owner,
        Transform transform,
        int slot,
        float now,
        float spawnedAt,
        float groupDuration,
        int spawnedCount,
        int maxCount,
        Renderer renderer)
    {
        if (!ShouldTraceUpline(groupName, owner, transform))
        {
            return;
        }

        Material mat = renderer != null && renderer.materials != null && renderer.materials.Length > 0
            ? renderer.materials[0]
            : null;

        Debug.Log(
            $"[UPLINE_GROUP_TICK] group='{groupName}' slot={slot} now={now:F3}s groupAlive={(now - spawnedAt):F3}s " +
            $"groupDuration={groupDuration:F3}s spawned={spawnedCount}/{maxCount} " +
            $"visibleAge~={EstimateVisibleShaderAge(mat, now):F3}s fadeOutFrac from diag below " +
            $"[FADE_PHASE]={ShaderFadeDiagnostic.FadePhaseLabel(mat, now)} " +
            $"{ShaderFadeDiagnostic.BuildLine(mat, now)} frame={Time.frameCount}.");
    }
}
#endif
