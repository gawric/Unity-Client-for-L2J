#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

public struct UcRange
{
    public float Min;
    public float Max;

    public UcRange(float min, float max)
    {
        Min = min;
        Max = max;
    }
}

public struct UcVectorRange
{
    public UcRange X;
    public UcRange Y;
    public UcRange Z;

    public UcVectorRange(UcRange x, UcRange y, UcRange z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static UcVectorRange Uniform(float value)
    {
        var range = new UcRange(value, value);
        return new UcVectorRange(range, range, range);
    }
}

public struct UcColorScaleKey
{
    public int Index;
    public float RelativeTime;
    public Color Color;
}

public struct UcSizeScaleKey
{
    public int Index;
    public float RelativeTime;
    public float RelativeSize;
}

public sealed class UcEmitterDefinition
{
    public string ClassName;
    public string EmitterName;
    public string ParticleSlotName;
    public string StaticMeshReference;
    public int MaxParticles = 10;
    public bool HasInitialParticlesPerSecond;
    public int InitialParticlesPerSecond;
    public bool HasLifetimeRange;
    public float LifetimeMin;
    public float LifetimeMax;
    public bool HasInitialDelayRange;
    public float InitialDelayMin;
    public float InitialDelayMax;
    public bool HasRelativeWarmupTime;
    public float RelativeWarmupTime;
    public bool HasWarmupTicksPerSecond;
    public float WarmupTicksPerSecond;

    public string TextureReference;
    public string DrawStyle = "PTDS_Translucent";
    public string StartLocationShape;
    public string UseDirectionAs;
    public string GetVelocityDirectionFrom;
    public string UseRotationFrom;
    public bool HasProjectionNormal;
    public Vector3 ProjectionNormal;
    public bool HasBeamEndOffset;
    public UcVectorRange BeamEndOffset = UcVectorRange.Uniform(0f);
    public string DetermineEndPointBy;
    public int HighFrequencyPoints = 2;
    public int LowFrequencyPoints = 2;

    public bool RenderTwoSided;
    public bool SpinParticles;
    public bool UniformSize;
    public bool UseSizeScale;
    public bool FadeIn;
    public bool FadeOut;
    public bool UseRandomSubdivision;
    public bool BlendBetweenSubdivisions;
    public int AddLocationFromOtherEmitter = -1;
    public bool RespawnDeadParticles = true;

    public float Opacity = 1f;
    public float FadeInEndTime;
    public float FadeOutStartTime;
    public float ColorScaleRepeats;
    public float SizeScaleRepeats;
    public int TextureUSubdivisions = 1;
    public int TextureVSubdivisions = 1;
    public int SubdivisionStart;
    public int SubdivisionEnd;

    public Vector3 Acceleration;
    public Vector3 StartLocationOffset;
    public Vector3 SpinCcwOrCw = new Vector3(0.5f, 0.5f, 0.5f);
    public UcVectorRange StartLocationRange = UcVectorRange.Uniform(0f);
    public UcVectorRange StartLocationPolarRange = UcVectorRange.Uniform(0f);
    public UcVectorRange StartVelocityRange = UcVectorRange.Uniform(0f);
    public UcVectorRange VelocityLossRange = UcVectorRange.Uniform(0f);
    public UcVectorRange StartSizeRange = UcVectorRange.Uniform(1f);
    public UcVectorRange StartSpinRange = UcVectorRange.Uniform(0f);
    public UcVectorRange SpinsPerSecondRange = UcVectorRange.Uniform(0f);
    public UcVectorRange ColorMultiplierRange = UcVectorRange.Uniform(1f);
    public readonly List<UcColorScaleKey> ColorScaleKeys = new List<UcColorScaleKey>();
    public readonly List<UcSizeScaleKey> SizeScaleKeys = new List<UcSizeScaleKey>();

    /// <summary>
    /// UE default when LifetimeRange is omitted from the UC.
    /// </summary>
    public const float DefaultLifetimeSeconds = 1f;

    /// <summary>
    /// Extra seconds after FadeOutStart so subtractive fade-out has room
    /// when LifetimeRange was missing from the dump.
    /// </summary>
    public const float MissingLifetimeFadeOutTailSeconds = 0.16f;

    /// <summary>
    /// Authored LifetimeRange wins. If the UC omits it, start from the UE
    /// default 1s and lift only when FadeIn/FadeOut times would not finish.
    /// </summary>
    public void ResolveLifetimeRange(out float min, out float max)
    {
        if (HasLifetimeRange)
        {
            min = LifetimeMin;
            max = LifetimeMax;
            return;
        }

        float inferred = InferLifetimeWhenRangeMissing();
        min = inferred;
        max = inferred;
    }

    public bool HasInferredLifetimeFromFades()
    {
        return !HasLifetimeRange &&
               InferLifetimeWhenRangeMissing() > DefaultLifetimeSeconds + 1e-4f;
    }

    float InferLifetimeWhenRangeMissing()
    {
        float life = DefaultLifetimeSeconds;
        if (FadeIn && FadeInEndTime > 0f)
        {
            life = Math.Max(life, FadeInEndTime);
        }

        if (FadeOut && FadeOutStartTime > 0f)
        {
            float tail = MissingLifetimeFadeOutTailSeconds;
            // SizeScale is relative to life. A ~0.16s fade-out leaves the
            // mesh at RelativeSize(end) then respawns at key0 — a visible snap.
            if (UseSizeScale && FadeIn && FadeInEndTime > 0f)
            {
                tail = Math.Max(tail, FadeInEndTime);
            }

            life = Math.Max(life, FadeOutStartTime + tail);
        }

        return life;
    }
}

public sealed class UcFileInfo
{
    public string ClassName;
    public string ExtendsClass;
    public float Speed;
    public bool HasSpeed;
    public float AccSpeed;
    public bool HasAccSpeed;
    public readonly List<UcEmitterDefinition> Emitters = new List<UcEmitterDefinition>();

    public bool IsProjectile
    {
        get
        {
            return !string.IsNullOrEmpty(ExtendsClass) &&
                   ExtendsClass.IndexOf("Projectile", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
#endif
