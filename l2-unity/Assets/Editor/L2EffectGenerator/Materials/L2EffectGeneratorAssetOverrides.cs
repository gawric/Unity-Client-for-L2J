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

    // Per-slot MainTex names. One entry is applied to every slot; several
    // entries map 1:1 onto MeshEmitter materials (duplicates are kept).
    static readonly Dictionary<string, string[]> StaticMeshToTextures =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "windblowin00", new[] { "fx_m_t0001" } },
            { "windblowin01", new[] { "fx_m_t0001" } },
            { "black_vampire01", new[] { "fx_m_t0032", "fx_m_t0032", "fx_m_t0009" } },
        };

    // Slot 2 diamond: live TFactor + floor/contrast from RenderDoc
    // (vampiric_deprecate MeshEmitter34_2). Cull Off = diamond winding.
    static readonly Dictionary<string, MeshSlotShadingOverride> StaticMeshSlotShading =
        new Dictionary<string, MeshSlotShadingOverride>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "black_vampire01/2",
                new MeshSlotShadingOverride(
                    false,
                    true,
                    new Color(0.295f, 0.039f, 0.070f, 1f),
                    0.18f,
                    0.72f)
            },
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

    // UC StartLocationOffset Z=-14 is a mesh-local drop (~27cm). Spawn is already
    // TargetOverHead (nameplate); keeping the dump puts VampireEye in the ground.
    static readonly Dictionary<string, Vector3> EmitterStartLocationOffset =
        new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase)
        {
            { "m_u003_c/MeshEmitter34", Vector3.zero },
        };

    // Dump lists VampireBlink (SpriteEmitter10) on the trailer; live shot is
    // eye + flash only. The blink already flies on m_u003_b.
    static readonly HashSet<string> SkipEmitters =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "m_u003_c/SpriteEmitter10",
        };

    const string FxMt0005Cell6IsWrongAtlas = "fx_m_t0005";
    const int FxMt0005DumpedSubdivisionStart = 6;
    const int FxMt0005DumpedSubdivisionEnd = 8;
    const int FxMt0005CorrectSubdivisionStart = 7;
    const int FxMt0005CorrectSubdivisionEnd = 8;

    const string FxMt0005Atlas = "fx_m_t0005";
    const string FxMt0005Uv2CellTexture = "fx_m_t0005_A";
    const int FxMt0005Uv2 = 2;
    const int FxMt0005Uv2DumpedEnd = 3;
    const int FxMt0005Uv2Cell = 2;
    const float FxMt0005Uv2CellRgbBoost = 2f;

    const string FxMt0006Atlas = "fx_m_t0006";
    const string FxMt0006LinearAtlas = "fx_m_t0006_sRGB_Disabled";
    const int FxMt0006USub = 4;
    const int FxMt0006VSub = 2;
    const int FxMt0006DumpedEnd = 5;
    const int FxMt0006Cell = 4;

    const string FxMt0000Atlas = "fx_m_t0000";
    const int FxMt0000Uv = 4;
    const int FxMt0000DumpedStart = 14;
    const int FxMt0000DumpedEnd = 16;
    const int FxMt0000Cell = 15;

    const string FxMt0054Atlas = "fx_m_t0054";
    const string FxMt0054CellTexture = "fx_m_t0054_A";
    const int FxMt0054Uv = 4;
    const int FxMt0054DumpedEnd = 3;
    const int FxMt0054Cell = 2;
    const float FxMt0054RgbBoost = 0.44f;
    const float FxMt0054WorldCalibration = 0.9f;

    public readonly struct MeshSlotShadingOverride
    {
        public readonly bool IgnoreMainTexAlpha;
        public readonly bool CullOff;
        public readonly bool HasTexturePaint;
        public readonly Color TextureFactor;
        public readonly float TextureContrast;
        public readonly float TextureFloor;

        public MeshSlotShadingOverride(bool ignoreMainTexAlpha, bool cullOff)
            : this(ignoreMainTexAlpha, cullOff, Color.white, 1f, 0f, false)
        {
        }

        public MeshSlotShadingOverride(
            bool ignoreMainTexAlpha,
            bool cullOff,
            Color textureFactor,
            float textureContrast,
            float textureFloor)
            : this(ignoreMainTexAlpha, cullOff, textureFactor, textureContrast, textureFloor, true)
        {
        }

        MeshSlotShadingOverride(
            bool ignoreMainTexAlpha,
            bool cullOff,
            Color textureFactor,
            float textureContrast,
            float textureFloor,
            bool hasTexturePaint)
        {
            IgnoreMainTexAlpha = ignoreMainTexAlpha;
            CullOff = cullOff;
            TextureFactor = textureFactor;
            TextureContrast = textureContrast;
            TextureFloor = textureFloor;
            HasTexturePaint = hasTexturePaint;
        }
    }

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

    public static bool TryGetStartLocationOffset(
        string effectClassName,
        UcEmitterDefinition emitter,
        out Vector3 offset)
    {
        offset = Vector3.zero;
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

        return EmitterStartLocationOffset.TryGetValue(effect + "/" + emitterName, out offset);
    }

    public static bool ShouldSkipEmitter(string effectClassName, string emitterName)
    {
        if (string.IsNullOrWhiteSpace(effectClassName) ||
            string.IsNullOrWhiteSpace(emitterName))
        {
            return false;
        }

        return SkipEmitters.Contains(effectClassName.Trim() + "/" + emitterName.Trim());
    }

    public static bool TryGetTextureForStaticMesh(string staticMeshReference, out string textureName)
    {
        textureName = null;
        if (!TryGetTexturesForStaticMesh(staticMeshReference, out string[] names) ||
            names == null ||
            names.Length == 0)
        {
            return false;
        }

        textureName = names[0];
        return !string.IsNullOrWhiteSpace(textureName);
    }

    public static bool TryGetTexturesForStaticMesh(string staticMeshReference, out string[] textureNames)
    {
        textureNames = null;
        string meshName = GetUcObjectName(staticMeshReference);
        if (string.IsNullOrWhiteSpace(meshName))
        {
            return false;
        }

        return StaticMeshToTextures.TryGetValue(meshName, out textureNames);
    }

    public static bool TryGetMeshSlotShading(
        string staticMeshReference,
        int slotIndex,
        out MeshSlotShadingOverride shading)
    {
        shading = default;
        string meshName = GetUcObjectName(staticMeshReference);
        if (string.IsNullOrWhiteSpace(meshName) || slotIndex < 0)
        {
            return false;
        }

        return StaticMeshSlotShading.TryGetValue(meshName + "/" + slotIndex, out shading);
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

    /// <summary>
    /// 2x2 atlas, Texture=fx_m_t0005, cells 2..3 or 2..2: bind fx_m_t0005_A
    /// (not the sRGB dump) and Boost 2. Adjacent-pair collapse would turn
    /// 2..3 into cell 3 (wrong tile); pin both indices to cell 2.
    /// </summary>
    public static bool TryGetFxMt0005Uv2Cell23(
        UcEmitterDefinition emitter,
        out string textureName,
        out float rgbBoost)
    {
        textureName = null;
        rgbBoost = 1f;
        if (!IsFxMt0005Uv2Cell2Dump(emitter))
        {
            return false;
        }

        textureName = FxMt0005Uv2CellTexture;
        rgbBoost = FxMt0005Uv2CellRgbBoost;
        return true;
    }

    public static bool TryCorrectFxMt0005Uv2Cell2(
        UcEmitterDefinition emitter,
        ref int start,
        ref int end)
    {
        if (!IsFxMt0005Uv2Cell2Dump(emitter))
        {
            return false;
        }

        start = FxMt0005Uv2Cell;
        end = FxMt0005Uv2Cell;
        return true;
    }

    /// <summary>
    /// 4x4 atlas, Texture=fx_m_t0054, cells 2..3 or 2..2: bind fx_m_t0054_A,
    /// keep Start=2 End=3, sample static cell 2. World K 0.9 and Boost 0.44
    /// compensate Unity Linear AlphaBlend vs L2 UNORM (size + milk).
    /// Adjacent-pair collapse would turn 2..3 into timed cell 3.
    /// </summary>
    public static bool TryGetFxMt0054Uv44Cell2(
        UcEmitterDefinition emitter,
        out string textureName,
        out float rgbBoost,
        out float worldCalibration)
    {
        textureName = null;
        rgbBoost = 1f;
        worldCalibration = 0f;
        if (!IsFxMt0054Uv44Cell2Dump(emitter))
        {
            return false;
        }

        textureName = FxMt0054CellTexture;
        rgbBoost = FxMt0054RgbBoost;
        worldCalibration = FxMt0054WorldCalibration;
        return true;
    }

    public static bool TryCorrectFxMt0054Uv44Cell2(
        UcEmitterDefinition emitter,
        ref int start,
        ref int end)
    {
        if (!IsFxMt0054Uv44Cell2Dump(emitter))
        {
            return false;
        }

        start = FxMt0054Cell;
        end = FxMt0054DumpedEnd;
        return true;
    }

    /// <summary>
    /// 4x2 atlas, Texture=fx_m_t0006, cells 4..5 or 4..4: pin both to cell 4.
    /// Adjacent-pair collapse would pick cell 5 (wrong tile).
    /// </summary>
    public static bool TryCorrectFxMt0006Uv42Cell4(
        UcEmitterDefinition emitter,
        ref int start,
        ref int end)
    {
        if (!IsFxMt0006Uv42Cell4Dump(emitter))
        {
            return false;
        }

        start = FxMt0006Cell;
        end = FxMt0006Cell;
        return true;
    }

    /// <summary>
    /// Same dump as cell-4 pin: bind the linear atlas, not sRGB fx_m_t0006,
    /// and enable Color Gamma To Linear.
    /// </summary>
    public static bool TryGetFxMt0006Uv42LinearTexture(
        UcEmitterDefinition emitter,
        out string textureName,
        out float rgbBoost,
        out bool colorGammaToLinear)
    {
        textureName = null;
        rgbBoost = 1f;
        colorGammaToLinear = false;
        if (!IsFxMt0006Uv42Cell4Dump(emitter))
        {
            return false;
        }

        textureName = FxMt0006LinearAtlas;
        colorGammaToLinear = true;
        return true;
    }

    static bool IsFxMt0006Uv42Cell4Dump(UcEmitterDefinition emitter)
    {
        if (emitter == null ||
            emitter.TextureUSubdivisions != FxMt0006USub ||
            emitter.TextureVSubdivisions != FxMt0006VSub)
        {
            return false;
        }

        bool isDumpedPair =
            emitter.SubdivisionStart == FxMt0006Cell &&
            emitter.SubdivisionEnd == FxMt0006DumpedEnd;
        bool isPinnedCell =
            emitter.SubdivisionStart == FxMt0006Cell &&
            emitter.SubdivisionEnd == FxMt0006Cell;
        if (!isDumpedPair && !isPinnedCell)
        {
            return false;
        }

        return string.Equals(
            GetUcObjectName(emitter.TextureReference),
            FxMt0006Atlas,
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 4x4 atlas, Texture=fx_m_t0000, cells 14..16 or 15..15: pin both to
    /// cell 15. The dump range includes neighboring tiles.
    /// </summary>
    public static bool TryCorrectFxMt0000Uv44Cell15(
        UcEmitterDefinition emitter,
        ref int start,
        ref int end)
    {
        if (!IsFxMt0000Uv44Cell15Dump(emitter))
        {
            return false;
        }

        start = FxMt0000Cell;
        end = FxMt0000Cell;
        return true;
    }

    static bool IsFxMt0000Uv44Cell15Dump(UcEmitterDefinition emitter)
    {
        if (emitter == null ||
            emitter.TextureUSubdivisions != FxMt0000Uv ||
            emitter.TextureVSubdivisions != FxMt0000Uv)
        {
            return false;
        }

        bool isDumpedRange =
            emitter.SubdivisionStart == FxMt0000DumpedStart &&
            emitter.SubdivisionEnd == FxMt0000DumpedEnd;
        bool isPinnedCell =
            emitter.SubdivisionStart == FxMt0000Cell &&
            emitter.SubdivisionEnd == FxMt0000Cell;
        if (!isDumpedRange && !isPinnedCell)
        {
            return false;
        }

        return string.Equals(
            GetUcObjectName(emitter.TextureReference),
            FxMt0000Atlas,
            StringComparison.OrdinalIgnoreCase);
    }

    static bool IsFxMt0005Uv2Cell2Dump(UcEmitterDefinition emitter)
    {
        if (emitter == null ||
            emitter.TextureUSubdivisions != FxMt0005Uv2 ||
            emitter.TextureVSubdivisions != FxMt0005Uv2)
        {
            return false;
        }

        bool isDumpedPair =
            emitter.SubdivisionStart == FxMt0005Uv2Cell &&
            emitter.SubdivisionEnd == FxMt0005Uv2DumpedEnd;
        bool isPinnedCell =
            emitter.SubdivisionStart == FxMt0005Uv2Cell &&
            emitter.SubdivisionEnd == FxMt0005Uv2Cell;
        if (!isDumpedPair && !isPinnedCell)
        {
            return false;
        }

        return string.Equals(
            GetUcObjectName(emitter.TextureReference),
            FxMt0005Atlas,
            StringComparison.OrdinalIgnoreCase);
    }

    static bool IsFxMt0054Uv44Cell2Dump(UcEmitterDefinition emitter)
    {
        if (emitter == null ||
            emitter.TextureUSubdivisions != FxMt0054Uv ||
            emitter.TextureVSubdivisions != FxMt0054Uv)
        {
            return false;
        }

        bool isDumpedPair =
            emitter.SubdivisionStart == FxMt0054Cell &&
            emitter.SubdivisionEnd == FxMt0054DumpedEnd;
        bool isPinnedCell =
            emitter.SubdivisionStart == FxMt0054Cell &&
            emitter.SubdivisionEnd == FxMt0054Cell;
        if (!isDumpedPair && !isPinnedCell)
        {
            return false;
        }

        return string.Equals(
            GetUcObjectName(emitter.TextureReference),
            FxMt0054Atlas,
            StringComparison.OrdinalIgnoreCase);
    }

    const string FxMt0005StarAtlas = "fx_m_t0005_A";
    const float FxMt0005StarRgbBoost = 2f;

    /// <summary>
    /// Star cells 7..8 on the fx_m_t0005 family: bind fx_m_t0005_A and Boost 2.
    /// Linear additive cannot keep the blue plasma; this is the practical match.
    /// </summary>
    public static bool TryGetFxMt0005StarCell(
        UcEmitterDefinition emitter,
        Texture2D currentTexture,
        int subdivStart,
        int subdivEnd,
        out string textureName,
        out float rgbBoost)
    {
        textureName = null;
        rgbBoost = 1f;
        bool isStarRange =
            (subdivStart == FxMt0005CorrectSubdivisionStart &&
             subdivEnd == FxMt0005CorrectSubdivisionEnd) ||
            (subdivStart == FxMt0005DumpedSubdivisionStart &&
             subdivEnd == FxMt0005DumpedSubdivisionEnd);
        if (!isStarRange)
        {
            return false;
        }

        if (!IsFxMt0005Family(currentTexture != null ? currentTexture.name : null) &&
            !IsFxMt0005Family(GetUcObjectName(emitter != null ? emitter.TextureReference : null)))
        {
            return false;
        }

        textureName = FxMt0005StarAtlas;
        rgbBoost = FxMt0005StarRgbBoost;
        return true;
    }

    static bool IsFxMt0005Family(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return name.IndexOf("fx_m_t0005", StringComparison.OrdinalIgnoreCase) >= 0;
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

    // Outbound caster→target projectiles (wind_strike_fl, flame_strike_ra, …).
    // NSkillProjectile without these suffixes is home-flight (target→caster / FNMover).
    static readonly string[] OutboundProjectileSuffixes =
    {
        "_fl", "_ra", "_pr"
    };

    static readonly HashSet<string> TargetTrailerClasses =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "m_u003_c",
        };

    /// <summary>
    /// skill-effects lists CastingAction LineageEffect.m_u003_a for 1090/6689,
    /// but the vampiric folder only dumps m_u003_b (orbs) + m_u003_c (target).
    /// Live CA is the shared curse_poison prefab.
    /// </summary>
    public const string SharedBodyToMindCaClassName = "bl_body_to_mind_ca";
    public const string VampiricMissingCastingClassName = "m_u003_a";
    public const string SharedBodyToMindCaPrefabPath =
        "Assets/Resources/Data/Effects/curse_poison/bl_body_to_mind_ca/bl_body_to_mind_ca.prefab";
    public const int SharedBodyToMindCaLaunchSkillId = 1157;

    /// <summary>
    /// Locked 2026-09-01 after live FNMover capture + Unity playtest.
    /// UC Speed/AccSpeed are ignored. Regen of vampiric must keep these Unity meters.
    /// </summary>
    public const float M_u003_bLockedSpeed = 0.01f;
    public const float M_u003_bLockedAcceleration = 8.57f;
    public const float M_u003_bLockedMaxSpeed = 8.57f;
    public const float M_u003_bLockedPathSideOffset = 1.11f;
    public const float M_u003_bLockedPathHeightOffset = 1.35f;
    public const float M_u003_bLockedPathStartLineFactor = -0.2f;
    public const float M_u003_bLockedPathPeelAlongLine = 0.16f;
    public const float M_u003_bLockedPathApexAlongLine = 0.19f;
    public const float M_u003_bLockedPathEarlyClimbFactor = 1f;
    public const float M_u003_bLockedMaxLifetime = 2.5f;

    /// <summary>
    /// Live VampiricTrail.log 2026-09-01 22:13 — SpriteEmitter60/62 tail.
    /// ipsRaw@+0x1E8 reads 1.0; impliedIps = maxParticles/lifetime = 30.
    /// Trail length is orbSpeed * 0.333s (10cm at start, ~30cm mid-accel, ~2.5m at cruise).
    /// </summary>
    public const float M_u003_bLockedTrailHistorySeconds = 0.333f;
    public const float M_u003_bLockedTrailTravelSeconds = 0.333f;
    public const float M_u003_bLockedTrailHeadLagPercent = 0.05f;
    public const float M_u003_bLockedTrailFadeOutStart = 0.155f;
    public const float M_u003_bLockedTrailSparkSizeMeters = 2f / 52.5f;

    public static bool IsM_u003_bHomeFlight(string classOrFolderName)
    {
        string name = StripEffectClassName(classOrFolderName);
        return string.Equals(name, "m_u003_b", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsM_u003_bHomeFlight(
        L2EffectGeneratorFolderBuilder.PlannedFolder planned)
    {
        return planned != null &&
            (IsM_u003_bHomeFlight(planned.ClassName) ||
             IsM_u003_bHomeFlight(planned.FolderName));
    }

    /// <summary>
    /// Vampiric Touch visual: home orbs (m_u003_b) + target trailer (m_u003_c).
    /// Composite then prepends shared bl_body_to_mind_ca.
    /// </summary>
    public static bool IsVampiricTouchComposite(
        IReadOnlyList<L2EffectGeneratorFolderBuilder.PlannedFolder> plannedFolders)
    {
        if (plannedFolders == null)
        {
            return false;
        }

        bool hasHomeOrbs = false;
        bool hasTargetTrailer = false;
        for (int i = 0; i < plannedFolders.Count; i++)
        {
            L2EffectGeneratorFolderBuilder.PlannedFolder planned = plannedFolders[i];
            if (planned == null)
            {
                continue;
            }

            if (IsM_u003_bHomeFlight(planned))
            {
                hasHomeOrbs = true;
            }

            if (IsTargetTrailerEffect(planned.ClassName) ||
                IsTargetTrailerEffect(planned.FolderName))
            {
                hasTargetTrailer = true;
            }
        }

        return hasHomeOrbs && hasTargetTrailer;
    }

    public static bool PlannedHasEffectClass(
        IReadOnlyList<L2EffectGeneratorFolderBuilder.PlannedFolder> plannedFolders,
        string className)
    {
        string want = StripEffectClassName(className);
        if (plannedFolders == null || want.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < plannedFolders.Count; i++)
        {
            L2EffectGeneratorFolderBuilder.PlannedFolder planned = plannedFolders[i];
            if (planned == null)
            {
                continue;
            }

            if (string.Equals(StripEffectClassName(planned.ClassName), want, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(StripEffectClassName(planned.FolderName), want, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static bool ShouldPrependSharedBodyToMindCa(
        IReadOnlyList<L2EffectGeneratorFolderBuilder.PlannedFolder> plannedFolders)
    {
        return IsVampiricTouchComposite(plannedFolders) &&
               !PlannedHasEffectClass(plannedFolders, SharedBodyToMindCaClassName) &&
               !PlannedHasEffectClass(plannedFolders, VampiricMissingCastingClassName);
    }

    public static L2EffectGeneratorFolderBuilder.PlannedFolder CreateSharedBodyToMindCaPlanned()
    {
        return new L2EffectGeneratorFolderBuilder.PlannedFolder
        {
            FolderName = SharedBodyToMindCaClassName,
            ClassName = SharedBodyToMindCaClassName,
            Suffix = "_ca"
        };
    }

    public static L2EffectSkillLaunchTable.LaunchRow ResolveSharedBodyToMindCaRow(int skillVisualId)
    {
        List<L2EffectSkillLaunchTable.LaunchRow> missingCa =
            L2EffectSkillLaunchTable.RowsForComposite(
                skillVisualId,
                VampiricMissingCastingClassName,
                "_ca");
        if (missingCa.Count > 0)
        {
            return missingCa[0];
        }

        if (L2EffectSkillLaunchTable.TryFindRow(
                skillVisualId,
                SharedBodyToMindCaClassName,
                out L2EffectSkillLaunchTable.LaunchRow localCa))
        {
            return localCa;
        }

        L2EffectSkillLaunchTable.TryFindRow(
            SharedBodyToMindCaLaunchSkillId,
            SharedBodyToMindCaClassName,
            out L2EffectSkillLaunchTable.LaunchRow cursePoisonCa);
        return cursePoisonCa;
    }

    static string StripEffectClassName(string classOrFolderName)
    {
        if (string.IsNullOrWhiteSpace(classOrFolderName))
        {
            return string.Empty;
        }

        string name = classOrFolderName.Trim();
        int dot = name.LastIndexOf('.');
        if (dot >= 0 && dot < name.Length - 1)
        {
            name = name.Substring(dot + 1);
        }

        return name;
    }

    public static void ApplyM_u003_bLiveHomeFlight(CompositeHomeProjectileConfig home)
    {
        if (home == null)
        {
            return;
        }

        home.speed = M_u003_bLockedSpeed;
        home.acceleration = M_u003_bLockedAcceleration;
        home.maxSpeed = M_u003_bLockedMaxSpeed;
        home.pathSideOffset = M_u003_bLockedPathSideOffset;
        home.pathHeightOffset = M_u003_bLockedPathHeightOffset;
        home.pathDistanceHeightFactor = 0f;
        home.pathStartLineFactor = M_u003_bLockedPathStartLineFactor;
        home.pathPeelAlongLine = M_u003_bLockedPathPeelAlongLine;
        home.pathApexAlongLine = M_u003_bLockedPathApexAlongLine;
        home.pathPeakHeightAlongLine = 0f;
        home.pathEarlyClimbFactor = M_u003_bLockedPathEarlyClimbFactor;
        home.pathAscentSpeedScale = 1f;
        home.pathDescentSpeedScale = 1f;
        home.maxLifetime = M_u003_bLockedMaxLifetime;
        home.mirrorDualFlight = true;
        home.usePathArc = true;
        home.rotateToVelocity = true;
        home.destroyOnArrive = true;
        home.homeAttachmentPoint = EffectAttachmentPoint.CasterCenter;
        home.homeOffset = new Vector3(0f, 0.1f, 0f);
    }

    public static void ApplyM_u003_bLiveHomeFlight(HomeFlightPart home)
    {
        if (home == null)
        {
            return;
        }

        home.speed = M_u003_bLockedSpeed;
        home.acceleration = M_u003_bLockedAcceleration;
        home.maxSpeed = M_u003_bLockedMaxSpeed;
        home.pathSideOffset = M_u003_bLockedPathSideOffset;
        home.pathHeightOffset = M_u003_bLockedPathHeightOffset;
        home.pathDistanceHeightFactor = 0f;
        home.pathStartLineFactor = M_u003_bLockedPathStartLineFactor;
        home.pathPeelAlongLine = M_u003_bLockedPathPeelAlongLine;
        home.pathApexAlongLine = M_u003_bLockedPathApexAlongLine;
        home.pathPeakHeightAlongLine = 0f;
        home.pathEarlyClimbFactor = M_u003_bLockedPathEarlyClimbFactor;
        home.pathAscentSpeedScale = 1f;
        home.pathDescentSpeedScale = 1f;
        home.maxLifetime = M_u003_bLockedMaxLifetime;
        home.mirrorDualFlight = true;
        home.usePathArc = true;
        home.rotateToVelocity = true;
        home.destroyOnArrive = true;
        home.homeAttachmentPoint = EffectAttachmentPoint.CasterCenter;
        home.homeOffset = new Vector3(0f, 0.1f, 0f);
    }

    /// <summary>
    /// Target→caster orb. UC: extends NSkillProjectile and is not an outbound _fl/_ra/_pr shot.
    /// </summary>
    public static bool IsHomeFlightProjectile(string className, string extendsClass)
    {
        if (IsM_u003_bHomeFlight(className))
        {
            return true;
        }

        if (!IsSkillProjectileClass(extendsClass))
        {
            return false;
        }

        string name = (className ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < OutboundProjectileSuffixes.Length; i++)
        {
            if (name.EndsWith(OutboundProjectileSuffixes[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Hit-pawn trailer (m_u003_c): ShotAction attach_on=9 on the skill target.
    /// PHYS_Trailer only means "follow whoever we are attached to" — caster CA
    /// trailers (m_u004_a / m_u008_a) must not be treated as target shots.
    /// </summary>
    public static bool IsTargetTrailerEffect(string className)
    {
        string name = (className ?? string.Empty).Trim();
        return name.Length > 0 && TargetTrailerClasses.Contains(name);
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
