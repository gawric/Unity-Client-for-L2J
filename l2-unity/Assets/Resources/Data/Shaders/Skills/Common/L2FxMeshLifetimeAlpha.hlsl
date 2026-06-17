#ifndef L2_FX_MESH_LIFETIME_ALPHA_INCLUDED
#define L2_FX_MESH_LIFETIME_ALPHA_INCLUDED

// Mesh emitter lifetime alpha with L2SkillEffect _Hold split.
// FadeIn: wall-clock elapsedAge (UE ForcedFade). FadeOut: motionAge when hold>0 is off only.

float L2Fx_MeshLifetimeAlphaHold(
    float motionAge,
    float elapsedAge,
    float lifetime,
    float hold,
    float hasLifetime,
    float fadeIn,
    float fadeInEndTime,
    float fadeOut,
    float fadeOutStartTime)
{
    lifetime = max(lifetime, 1e-4);
    motionAge = max(0.0, motionAge);
    elapsedAge = max(0.0, elapsedAge);

    float fadeAge = (hold > 0.0) ? motionAge : elapsedAge;

    if (hasLifetime >= 0.5 && hold <= 0.0)
    {
        if (elapsedAge <= 0.0 || elapsedAge >= lifetime)
        {
            return 0.0;
        }
    }
    else if (hasLifetime >= 0.5 && hold > 0.0)
    {
        if (motionAge <= 0.0)
        {
            return 0.0;
        }
    }

    float fadeInMul = 1.0;
    if (fadeIn >= 0.5)
    {
        float fadeInEnd = max(0.0001, fadeInEndTime);
        fadeInMul = saturate(elapsedAge / fadeInEnd);
    }

    float fadeOutMul = 1.0;
    if (fadeOut >= 0.5 && hold <= 0.0)
    {
        float fadeStart = clamp(fadeOutStartTime, 0.0, lifetime);
        float fadeDuration = max(0.0001, lifetime - fadeStart);
        float fadeT = saturate((fadeAge - fadeStart) / fadeDuration);
        fadeOutMul = 1.0 - fadeT;
    }

    return saturate(fadeInMul * fadeOutMul);
}

#endif // L2_FX_MESH_LIFETIME_ALPHA_INCLUDED
