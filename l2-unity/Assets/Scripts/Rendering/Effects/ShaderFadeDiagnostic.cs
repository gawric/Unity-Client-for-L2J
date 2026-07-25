using UnityEngine;

/// <summary>
/// Диагностика L2-шейдерных полей fade (материал + Time). Логи — только Editor / Development Build.
/// </summary>
public static class ShaderFadeDiagnostic
{
    private static readonly int HasLifetimeShaderId = Shader.PropertyToID("_HasLifetime");
    private static readonly int HoldShaderId = Shader.PropertyToID("_Hold");
    private static readonly int FadeInShaderId = Shader.PropertyToID("_FadeIn");
    private static readonly int FadeInEndTimeShaderId = Shader.PropertyToID("_FadeInEndTime");
    private static readonly int FadeoutShaderId = Shader.PropertyToID("_Fadeout");
    private static readonly int FadeoutStartTimeShaderId = Shader.PropertyToID("_FadeoutStartTime");
    private static readonly int LifetimeRangeShaderId = Shader.PropertyToID("_LifetimeRange");
    private static readonly int StartTimeShaderId = Shader.PropertyToID("_StartTime");
    private static readonly int InitialDelayRangeShaderId = Shader.PropertyToID("_InitialDelayRange");
    private static readonly int DebugMeshPreviewShaderId = Shader.PropertyToID("_DebugMeshPreview");
    private static readonly int DebugMeshPreviewLoopShaderId = Shader.PropertyToID("_DebugMeshPreviewLoop");
    private static readonly int OpacityShaderId = Shader.PropertyToID("_Opacity");
    private static readonly int EmitterAlphaShaderId = Shader.PropertyToID("_EmitterAlpha");

    public static string BuildLine(Material mat, float now)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (mat == null)
        {
            return "null_mat";
        }

        string sn = mat.shader != null ? mat.shader.name : "no_shader";
        float hasLt = mat.HasProperty(HasLifetimeShaderId) ? mat.GetFloat(HasLifetimeShaderId) : -1f;
        float fadeout = mat.HasProperty(FadeoutShaderId) ? mat.GetFloat(FadeoutShaderId) : -1f;
        float fadeIn = mat.HasProperty(FadeInShaderId) ? mat.GetFloat(FadeInShaderId) : -1f;
        float fadeInEnd = mat.HasProperty(FadeInEndTimeShaderId) ? mat.GetFloat(FadeInEndTimeShaderId) : -1f;
        float fadeStart = mat.HasProperty(FadeoutStartTimeShaderId) ? mat.GetFloat(FadeoutStartTimeShaderId) : -1f;
        Vector4 life = mat.HasProperty(LifetimeRangeShaderId) ? mat.GetVector(LifetimeRangeShaderId) : Vector4.zero;
        float lifeMax = Mathf.Max(life.x, life.y, 1e-6f);
        float hold = mat.HasProperty(HoldShaderId) ? mat.GetFloat(HoldShaderId) : -1f;
        float st = mat.HasProperty(StartTimeShaderId) ? mat.GetFloat(StartTimeShaderId) : -1f;
        float age = st > -0.5f ? now - st : -1f;
        Vector4 idr = mat.HasProperty(InitialDelayRangeShaderId) ? mat.GetVector(InitialDelayRangeShaderId) : Vector4.zero;
        float delayMid = (Mathf.Min(idr.x, idr.y) + Mathf.Max(idr.x, idr.y)) * 0.5f;
        float ageVisMid = age >= 0f ? age - delayMid : age;
        float holdAnimAge = (hold > 1e-4f && ageVisMid >= 0f) ? Mathf.Min(ageVisMid, lifeMax * hold) : -1f;
        float debugPreview = mat.HasProperty(DebugMeshPreviewShaderId) ? mat.GetFloat(DebugMeshPreviewShaderId) : -1f;
        float debugLoop = mat.HasProperty(DebugMeshPreviewLoopShaderId) ? mat.GetFloat(DebugMeshPreviewLoopShaderId) : -1f;
        float opacity = mat.HasProperty(OpacityShaderId) ? mat.GetFloat(OpacityShaderId) : -1f;
        float emitterAlpha = mat.HasProperty(EmitterAlphaShaderId) ? mat.GetFloat(EmitterAlphaShaderId) : -1f;
        float loopAge = (hasLt < 0.5f && age >= 0f) ? Mathf.Repeat(age, lifeMax) : -1f;
        float fadeAge = holdAnimAge >= 0f ? holdAnimAge : (loopAge >= 0f ? loopAge : ageVisMid >= 0f ? ageVisMid : age);
        float tail = (fadeStart >= 0f && lifeMax > 1e-4f) ? lifeMax - fadeStart : -1f;
        float fadeFrac =
            tail > 1e-4f && fadeAge >= fadeStart
                ? Mathf.Clamp01((fadeAge - fadeStart) / tail)
                : -1f;
        return
            $"{mat.name} shader={sn} HasLt={hasLt} FadeInOn={fadeIn} FadeInEnd={fadeInEnd:F4} " +
            $"FadeoutOn={fadeout} fadeOutStart={fadeStart:F4} lifeMax={lifeMax:F4} tail={tail:F4} Hold={hold} " +
            $"initDelay=({idr.x:F3},{idr.y:F3}) StartT={st:F4} age={age:F4}s holdAnimAge={(holdAnimAge >= 0f ? holdAnimAge.ToString("F4") : "-1")} loopAge={loopAge:F4}s " +
            $"DebugPreview={debugPreview:F0} DebugLoop={debugLoop:F0} Opacity={opacity:F3} EmitterA={emitterAlpha:F3} " +
            $"fadeOutFrac~={fadeFrac:F3}";
#else
        return string.Empty;
#endif
    }

    /// <summary>
    /// Грубая метка фазы по числам в материале (шейдер может отличаться). Ищите в Console: [FADE_PHASE].
    /// </summary>
    public static string FadePhaseLabel(Material mat, float now)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (mat == null)
        {
            return "null_mat";
        }

        float hasLt = mat.HasProperty(HasLifetimeShaderId) ? mat.GetFloat(HasLifetimeShaderId) : 1f;
        if (hasLt < 0.5f)
        {
            float st0 = mat.HasProperty(StartTimeShaderId) ? mat.GetFloat(StartTimeShaderId) : -1f;
            if (st0 < -0.49f)
            {
                return "NO_SHADER_LIFETIME";
            }

            float age0 = now - st0;
            Vector4 life0 = mat.HasProperty(LifetimeRangeShaderId) ? mat.GetVector(LifetimeRangeShaderId) : Vector4.zero;
            float lifeMax0 = Mathf.Max(life0.x, life0.y, 1e-6f);
            float loopAge0 = Mathf.Repeat(Mathf.Max(0f, age0), lifeMax0);
            float fadeInOn0 = mat.HasProperty(FadeInShaderId) ? mat.GetFloat(FadeInShaderId) : 0f;
            float fadeInEnd0 = mat.HasProperty(FadeInEndTimeShaderId) ? mat.GetFloat(FadeInEndTimeShaderId) : 0f;
            float fadeOutOn0 = mat.HasProperty(FadeoutShaderId) ? mat.GetFloat(FadeoutShaderId) : 0f;
            float fadeOutStart0 = mat.HasProperty(FadeoutStartTimeShaderId) ? mat.GetFloat(FadeoutStartTimeShaderId) : float.MaxValue;

            if (fadeInOn0 >= 0.5f && loopAge0 < fadeInEnd0)
            {
                return "NO_SHADER_LIFETIME_FADE_IN";
            }

            if (fadeOutOn0 >= 0.5f && loopAge0 >= fadeOutStart0)
            {
                return "NO_SHADER_LIFETIME_FADE_OUT";
            }

            return "NO_SHADER_LIFETIME_FULL";
        }

        float st = mat.HasProperty(StartTimeShaderId) ? mat.GetFloat(StartTimeShaderId) : -1f;
        if (st < -0.49f)
        {
            return "no_start_time";
        }

        float age = now - st;
        Vector4 idr = mat.HasProperty(InitialDelayRangeShaderId) ? mat.GetVector(InitialDelayRangeShaderId) : Vector4.zero;
        float delayMin = Mathf.Min(idr.x, idr.y);
        float delayMax = Mathf.Max(idr.x, idr.y);
        float ageVisMid = age - (delayMin + delayMax) * 0.5f;

        Vector4 life = mat.HasProperty(LifetimeRangeShaderId) ? mat.GetVector(LifetimeRangeShaderId) : Vector4.zero;
        float lifeMax = Mathf.Max(life.x, life.y, 1e-6f);

        float fadeInOn = mat.HasProperty(FadeInShaderId) ? mat.GetFloat(FadeInShaderId) : 0f;
        float fadeInEnd = mat.HasProperty(FadeInEndTimeShaderId) ? mat.GetFloat(FadeInEndTimeShaderId) : 0f;
        float fadeOutOn = mat.HasProperty(FadeoutShaderId) ? mat.GetFloat(FadeoutShaderId) : 0f;
        float fadeOutStart = mat.HasProperty(FadeoutStartTimeShaderId) ? mat.GetFloat(FadeoutStartTimeShaderId) : float.MaxValue;
        float hold = mat.HasProperty(HoldShaderId) ? mat.GetFloat(HoldShaderId) : 0f;

        if (age < delayMax - 1e-4f)
        {
            return "INITIAL_DELAY";
        }

        // Hold caps anim age at lifeMax * hold — no shader expiry / fade-out tail while hold is active.
        if (hold > 1e-4f)
        {
            float heldAge = Mathf.Min(Mathf.Max(0f, ageVisMid), lifeMax * hold);
            if (fadeInOn >= 0.5f && heldAge < fadeInEnd)
            {
                return "HOLD_FADE_IN";
            }

            return "HOLD_FULL";
        }

        if (fadeInOn >= 0.5f && ageVisMid < fadeInEnd)
        {
            return "FADE_IN";
        }

        if (ageVisMid >= lifeMax - 1e-4f)
        {
            return "SHADER_LIFE_EXPIRED";
        }

        if (fadeOutOn >= 0.5f && ageVisMid >= fadeOutStart)
        {
            return fadeOutStart >= lifeMax - 1e-4f ? "FADEOUT_START_AFTER_OR_AT_LIFE_MAX" : "FADE_OUT";
        }

        return "FULL";
#else
        return string.Empty;
#endif
    }

    public static string BuildRendererVisibilityLine(Renderer renderer, Material mat, float now)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (renderer == null)
        {
            return "renderer=null";
        }

        Bounds bounds = renderer.bounds;
        Texture mainTex = mat != null && mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
        return
            $"renderer='{renderer.name}' enabled={renderer.enabled} goActive={renderer.gameObject.activeInHierarchy} " +
            $"pos={renderer.transform.position} scale={renderer.transform.lossyScale} " +
            $"boundsSize={bounds.size} mainTex={(mainTex != null ? mainTex.name : "null")} " +
            BuildLine(mat, now);
#else
        return string.Empty;
#endif
    }
}
