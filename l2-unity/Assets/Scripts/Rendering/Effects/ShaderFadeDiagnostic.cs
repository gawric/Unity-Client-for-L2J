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
        float tail = (fadeStart >= 0f && lifeMax > 1e-4f) ? lifeMax - fadeStart : -1f;
        float fadeFrac =
            tail > 1e-4f && age >= fadeStart
                ? Mathf.Clamp01((age - fadeStart) / tail)
                : -1f;
        return
            $"{mat.name} shader={sn} HasLt={hasLt} FadeInOn={fadeIn} FadeInEnd={fadeInEnd:F4} " +
            $"FadeoutOn={fadeout} fadeOutStart={fadeStart:F4} lifeMax={lifeMax:F4} tail={tail:F4} Hold={hold} " +
            $"initDelay=({idr.x:F3},{idr.y:F3}) StartT={st:F4} age={age:F4}s fadeOutFrac~={fadeFrac:F3}";
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

        if (age < delayMax - 1e-4f)
        {
            return "INITIAL_DELAY";
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
}
