#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Corrections for decompiler mistakes in .uc / FBX materials, and
/// projectile-basis mapping that UC cannot express.
/// </summary>
public static class L2EffectGeneratorAssetOverrides
{
    const float ActorXEpsilon = 1e-8f;

    static readonly Dictionary<string, string> StaticMeshToTexture =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "windblowin00", "fx_m_t0001" },
            { "windblowin01", "fx_m_t0001" },
        };

    // Opt-out if a projectile emitter must keep raw UC X.
    static readonly HashSet<string> SkipAlignActorXWithProjectileFlight =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Per-effect draw order. Lower queue draws first (behind).
    /// bl_body_to_mind_ca: Mesh3 < DarkMatter < circle/aura/sprites.
    /// </summary>
    static readonly Dictionary<string, DrawOrderOverride> EmitterDrawOrder =
        new Dictionary<string, DrawOrderOverride>(StringComparer.OrdinalIgnoreCase)
        {
            { "bl_body_to_mind_ca/MeshEmitter3", new DrawOrderOverride(2998, -1) },
            { "bl_body_to_mind_ca/MeshEmitter0", new DrawOrderOverride(2999, 0) },
            { "bl_body_to_mind_ca/MeshEmitter1", new DrawOrderOverride(3000, 1) },
            { "bl_body_to_mind_ca/MeshEmitter2", new DrawOrderOverride(3000, 1) },
            { "bl_body_to_mind_ca/SpriteEmitter4", new DrawOrderOverride(3000, 1) },
        };

    const string FxMt0005Cell6IsWrongAtlas = "fx_m_t0005";
    const int FxMt0005DumpedSubdivisionStart = 6;
    const int FxMt0005DumpedSubdivisionEnd = 8;
    const int FxMt0005CorrectSubdivisionStart = 7;
    const int FxMt0005CorrectSubdivisionEnd = 8;

    public readonly struct DrawOrderOverride
    {
        public readonly int RenderQueue;
        public readonly int SortingOrder;

        public DrawOrderOverride(int renderQueue, int sortingOrder)
        {
            RenderQueue = renderQueue;
            SortingOrder = sortingOrder;
        }
    }

    public static bool TryGetDrawOrder(
        string effectClassName,
        UcEmitterDefinition emitter,
        out DrawOrderOverride drawOrder)
    {
        drawOrder = default;
        if (emitter == null)
        {
            return false;
        }

        string effect = (effectClassName ?? string.Empty).Trim();
        string emitterName = (emitter.EmitterName ?? string.Empty).Trim();
        if (effect.Length == 0 || emitterName.Length == 0)
        {
            return false;
        }

        return EmitterDrawOrder.TryGetValue(effect + "/" + emitterName, out drawOrder);
    }

    public static bool TryGetTextureForStaticMesh(string staticMeshReference, out string textureName)
    {
        textureName = null;
        string meshName = GetUcObjectName(staticMeshReference);
        if (string.IsNullOrWhiteSpace(meshName))
        {
            return false;
        }

        return StaticMeshToTexture.TryGetValue(meshName, out textureName);
    }

    /// <summary>
    /// fx_m_t0005 4x4: UC often dumps SubdivisionStart=6 End=8, but cell 6
    /// is a different atlas tile. Keep only 7..8 when that exact dump matches.
    /// </summary>
    public static bool TryCorrectFxMt0005Subdivision68(
        UcEmitterDefinition emitter,
        ref int start,
        ref int end)
    {
        if (emitter == null ||
            start != FxMt0005DumpedSubdivisionStart ||
            end != FxMt0005DumpedSubdivisionEnd)
        {
            return false;
        }

        string textureName = GetUcObjectName(emitter.TextureReference);
        if (!string.Equals(textureName, FxMt0005Cell6IsWrongAtlas, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        start = FxMt0005CorrectSubdivisionStart;
        end = FxMt0005CorrectSubdivisionEnd;
        return true;
    }

    public static string GetUcObjectName(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return string.Empty;
        }

        int dot = reference.LastIndexOf('.');
        return dot >= 0 ? reference.Substring(dot + 1) : reference.Trim();
    }

    public static bool IsSkillProjectileClass(string extendsClass)
    {
        return !string.IsNullOrWhiteSpace(extendsClass) &&
               extendsClass.IndexOf("Projectile", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// ProjectileManager maps imported mesh +X onto flight (LookRotation * +90).
    /// UC actor-X motion is written into that same object space, so raw UC -X
    /// travels anti-flight. Align X for NSkillProjectile emitters that actually
    /// move on actor X and do not use PTVD (radial from owner).
    /// Detected from UC `extends …Projectile`, not a per-emitter name list.
    /// </summary>
    public static bool ShouldAlignActorXWithProjectileFlight(
        string effectClassName,
        string extendsClass,
        UcEmitterDefinition emitter)
    {
        if (emitter == null || !IsSkillProjectileClass(extendsClass))
        {
            return false;
        }

        if (HasPtvd(emitter.GetVelocityDirectionFrom))
        {
            return false;
        }

        if (!HasActorXMotion(emitter))
        {
            return false;
        }

        string key = (effectClassName ?? string.Empty).Trim() + "/" +
                     (emitter.EmitterName ?? string.Empty).Trim();
        return !SkipAlignActorXWithProjectileFlight.Contains(key);
    }

    static bool HasPtvd(string getVelocityDirectionFrom)
    {
        return !string.IsNullOrWhiteSpace(getVelocityDirectionFrom) &&
               getVelocityDirectionFrom.StartsWith("PTVD_", StringComparison.OrdinalIgnoreCase);
    }

    static bool HasActorXMotion(UcEmitterDefinition emitter)
    {
        if (Mathf.Abs(emitter.StartLocationOffset.x) > ActorXEpsilon ||
            Mathf.Abs(emitter.Acceleration.x) > ActorXEpsilon)
        {
            return true;
        }

        return Mathf.Abs(emitter.StartVelocityRange.X.Min) > ActorXEpsilon ||
               Mathf.Abs(emitter.StartVelocityRange.X.Max) > ActorXEpsilon;
    }
}
#endif
