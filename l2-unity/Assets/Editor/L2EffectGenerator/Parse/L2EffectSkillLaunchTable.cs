#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// HF268 LineageSkillEffect launch table (skill-effects.tsv).
/// skill_id is skill_visual_effect, not always the server skill id.
/// </summary>
public static class L2EffectSkillLaunchTable
{
    public const string DefaultAssetPath = "Assets/Editor/L2EffectGenerator/Data/skill-effects.tsv";
    public const float UnrealSpeedToUnity = 0.01f;

    public sealed class LaunchRow
    {
        public int SkillId;
        public string Phase;
        public string EffectClass;
        public int AttachOn;
        public bool HasAttachOn;
        public string Bone;
        public float SpawnDelay;
        public bool HasSpawnDelay;
        public float Scale;
        public bool HasScale;
        public bool OnTarget;
    }

    private static List<LaunchRow> _rows;
    private static string _loadedPath;
    private static DateTime _loadedWriteTimeUtc;

    public static string ResolveTablePath()
    {
        if (File.Exists(GetFullPath(DefaultAssetPath)))
        {
            return DefaultAssetPath;
        }

        string[] guids = AssetDatabase.FindAssets("skill-effects t:DefaultAsset");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!string.IsNullOrEmpty(path) &&
                path.EndsWith("skill-effects.tsv", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }
        }

        return DefaultAssetPath;
    }

    public static bool TryLoad(out string errorMessage)
    {
        errorMessage = null;
        string assetPath = ResolveTablePath();
        string fullPath = GetFullPath(assetPath);
        if (!File.Exists(fullPath))
        {
            errorMessage = "Launch table not found: " + assetPath;
            _rows = null;
            return false;
        }

        DateTime writeTime = File.GetLastWriteTimeUtc(fullPath);
        if (_rows != null &&
            string.Equals(_loadedPath, fullPath, StringComparison.OrdinalIgnoreCase) &&
            writeTime == _loadedWriteTimeUtc)
        {
            return true;
        }

        _rows = ParseTsv(File.ReadAllLines(fullPath));
        _loadedPath = fullPath;
        _loadedWriteTimeUtc = writeTime;
        if (_rows.Count == 0)
        {
            errorMessage = "Launch table is empty: " + assetPath;
            return false;
        }

        return true;
    }

    public static string ToEffectClassKey(string className)
    {
        if (string.IsNullOrWhiteSpace(className))
        {
            return string.Empty;
        }

        string trimmed = className.Trim();
        if (trimmed.StartsWith("LineageEffect.", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return "LineageEffect." + trimmed;
    }

    public static bool TryFindRow(int skillId, string className, out LaunchRow row)
    {
        row = null;
        List<LaunchRow> rows = FindRows(skillId, className);
        if (rows.Count == 0)
        {
            return false;
        }

        row = rows[0];
        return true;
    }

    public static List<LaunchRow> FindRows(int skillId, string className)
    {
        var matches = new List<LaunchRow>();
        int tableId = ResolveLaunchTableId(skillId);
        if (tableId <= 0 || !TryLoad(out _))
        {
            return matches;
        }

        string key = ToEffectClassKey(className);
        for (int i = 0; i < _rows.Count; i++)
        {
            LaunchRow candidate = _rows[i];
            if (candidate.SkillId == tableId &&
                string.Equals(candidate.EffectClass, key, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(candidate);
            }
        }

        return matches;
    }

    /// <summary>
    /// skill-effects.tsv is keyed by skill_visual_effect, not always the server skill id.
    /// 1147 Vampiric Touch → visual 1090.
    /// </summary>
    public static int ResolveLaunchTableId(int skillOrVisualId)
    {
        if (skillOrVisualId <= 0 || !TryLoad(out _))
        {
            return skillOrVisualId;
        }

        if (HasAnyLaunchRow(skillOrVisualId))
        {
            return skillOrVisualId;
        }

        if (L2EffectSkillgrpVisualMap.TryGetVisualEffect(skillOrVisualId, out int visual) &&
            visual > 0 &&
            visual != skillOrVisualId &&
            HasAnyLaunchRow(visual))
        {
            return visual;
        }

        return skillOrVisualId;
    }

    static bool HasAnyLaunchRow(int skillId)
    {
        if (_rows == null)
        {
            return false;
        }

        for (int i = 0; i < _rows.Count; i++)
        {
            if (_rows[i].SkillId == skillId)
            {
                return true;
            }
        }

        return false;
    }

    public static List<LaunchRow> RowsForComposite(int skillId, string className, string suffix)
    {
        List<LaunchRow> found = FindRows(skillId, className);
        List<LaunchRow> rows = UniqueRows(found);
        if (!IsImpactSuffix(suffix))
        {
            return rows;
        }

        bool hasShot = false;
        for (int i = 0; i < rows.Count; i++)
        {
            if (string.Equals(rows[i].Phase, "ShotAction", StringComparison.OrdinalIgnoreCase))
            {
                hasShot = true;
                break;
            }
        }

        if (!hasShot)
        {
            return rows;
        }

        var filtered = new List<LaunchRow>();
        for (int i = 0; i < rows.Count; i++)
        {
            if (!string.Equals(rows[i].Phase, "CastingAction", StringComparison.OrdinalIgnoreCase))
            {
                filtered.Add(rows[i]);
            }
        }

        return filtered.Count > 0 ? filtered : rows;
    }

    static List<LaunchRow> UniqueRows(List<LaunchRow> rows)
    {
        var unique = new List<LaunchRow>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < rows.Count; i++)
        {
            LaunchRow row = rows[i];
            string key = row.Phase + "\t" +
                         row.EffectClass + "\t" +
                         (row.HasAttachOn ? row.AttachOn.ToString() : "-") + "\t" +
                         (row.Bone ?? string.Empty) + "\t" +
                         (row.HasSpawnDelay ? row.SpawnDelay.ToString("0.###") : "-") + "\t" +
                         (row.HasScale ? row.Scale.ToString("0.###") : "-") + "\t" +
                         (row.OnTarget ? "1" : "0");
            if (seen.Add(key))
            {
                unique.Add(row);
            }
        }

        return unique;
    }

    public sealed class SkillResolveResult
    {
        public int SkillId;
        public bool Ambiguous;
        public readonly List<int> Candidates = new List<int>();
    }

    public static SkillResolveResult ResolveSkillId(IList<string> classNames, int preferredSkillId)
    {
        var result = new SkillResolveResult();
        if (preferredSkillId > 0)
        {
            result.SkillId = preferredSkillId;
            return result;
        }

        if (classNames == null || classNames.Count == 0 || !TryLoad(out _))
        {
            return result;
        }

        var keys = new List<string>(classNames.Count);
        for (int i = 0; i < classNames.Count; i++)
        {
            string key = ToEffectClassKey(classNames[i]);
            if (!string.IsNullOrEmpty(key) && !keys.Contains(key))
            {
                keys.Add(key);
            }
        }

        if (keys.Count == 0)
        {
            return result;
        }

        var keySet = new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
        var matchedKeys = new Dictionary<int, HashSet<string>>();
        for (int i = 0; i < _rows.Count; i++)
        {
            LaunchRow row = _rows[i];
            if (!keySet.Contains(row.EffectClass))
            {
                continue;
            }

            HashSet<string> set;
            if (!matchedKeys.TryGetValue(row.SkillId, out set))
            {
                set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                matchedKeys[row.SkillId] = set;
            }

            set.Add(row.EffectClass);
        }

        foreach (KeyValuePair<int, HashSet<string>> pair in matchedKeys)
        {
            if (pair.Value.Count != keys.Count)
            {
                continue;
            }

            result.Candidates.Add(pair.Key);
        }

        result.Candidates.Sort();
        if (result.Candidates.Count == 1)
        {
            result.SkillId = result.Candidates[0];
        }
        else if (result.Candidates.Count > 1)
        {
            // m_u003_b/c match both 1090 and 6689 with the same ShotAction rows.
            // Prefer the lowest id so composite still gets table timing/attach.
            result.Ambiguous = true;
            if (CandidatesShareLaunchRows(result.Candidates, keys))
            {
                result.SkillId = result.Candidates[0];
            }
        }

        return result;
    }

    static bool CandidatesShareLaunchRows(List<int> skillIds, List<string> classKeys)
    {
        if (skillIds == null || skillIds.Count < 2 || classKeys == null || classKeys.Count == 0)
        {
            return false;
        }

        string signature = BuildLaunchSignature(skillIds[0], classKeys);
        for (int i = 1; i < skillIds.Count; i++)
        {
            if (!string.Equals(
                    signature,
                    BuildLaunchSignature(skillIds[i], classKeys),
                    StringComparison.Ordinal))
            {
                return false;
            }
        }

        return !string.IsNullOrEmpty(signature);
    }

    static string BuildLaunchSignature(int skillId, List<string> classKeys)
    {
        var parts = new List<string>();
        for (int k = 0; k < classKeys.Count; k++)
        {
            List<LaunchRow> rows = FindRows(skillId, classKeys[k]);
            for (int i = 0; i < rows.Count; i++)
            {
                LaunchRow row = rows[i];
                parts.Add(
                    row.Phase + "|" +
                    row.EffectClass + "|" +
                    (row.HasAttachOn ? row.AttachOn.ToString() : "-") + "|" +
                    (row.Bone ?? string.Empty) + "|" +
                    (row.HasSpawnDelay ? row.SpawnDelay.ToString("0.###") : "-") + "|" +
                    (row.OnTarget ? "1" : "0"));
            }
        }

        parts.Sort(StringComparer.Ordinal);
        return string.Join("\n", parts);
    }

    public static CompositePrefabPart CreatePart(
        L2EffectGeneratorFolderBuilder.PlannedFolder planned,
        LaunchRow row,
        bool hasProjectileCompanion)
    {
        var part = new CompositePrefabPart
        {
            name = planned != null ? planned.FolderName : string.Empty,
            inheritRotation = false,
            passCastDataToPart = true,
            passShaderTargetPosition = false,
            overrideContinuousLoop = false,
            continuousLoop = false,
            disableShaderLifetime = false,
            overrideHideTime = false,
            customHideTime = 0.5f,
            projectile = new CompositeProjectileConfig
            {
                launchMode = ProjectileLaunchMode.Disabled,
                showBeforeAnimationShoot = true,
                impactType = ProjectileImpactType.EffectOnly
            }
        };

        string suffix = planned != null ? planned.Suffix : null;
        bool isHomeFlight = planned != null &&
            (planned.IsHomeFlight ||
             L2EffectGeneratorAssetOverrides.IsM_u003_bHomeFlight(planned));
        bool isTargetTrailer = planned != null && planned.IsTargetTrailer;
        bool isProjectile = planned != null && planned.IsProjectile && !isHomeFlight;
        string phase = row != null ? row.Phase : null;

        part.spawnTiming = ResolveSpawnTiming(phase, suffix, hasProjectileCompanion, planned);
        part.attachmentPoint = ResolveAttachment(
            row, suffix, isProjectile, isHomeFlight, part.spawnTiming, planned, phase);
        part.followResolvedTransform = ShouldFollow(
            row, part.attachmentPoint, part.spawnTiming, isHomeFlight);
        part.useCastTimedLifetime = part.spawnTiming != CompositePartSpawnTiming.OnHitCollider &&
                                    part.spawnTiming != CompositePartSpawnTiming.OnAnimationShoot &&
                                    !IsImpactSuffix(suffix) &&
                                    !IsBlessingBeamPart(planned) &&
                                    !isHomeFlight &&
                                    !isTargetTrailer;
        part.scale = row != null && row.HasScale && row.Scale > 0f ? row.Scale : 1f;
        if (row != null && row.HasSpawnDelay && row.SpawnDelay > 0f)
        {
            part.spawnDelaySeconds = row.SpawnDelay;
        }

        if (row != null &&
            row.HasAttachOn &&
            row.AttachOn == 7 &&
            !string.IsNullOrWhiteSpace(row.Bone))
        {
            part.attachmentBoneName = row.Bone;
        }

        if (isHomeFlight)
        {
            ApplyHomeFlightConfig(part.homeProjectile, planned, part.spawnTiming);
        }
        else if (isProjectile)
        {
            bool spawnOnShoot = part.spawnTiming == CompositePartSpawnTiming.OnAnimationShoot;
            part.projectile.launchMode = spawnOnShoot
                ? ProjectileLaunchMode.OnAnimationShoot
                : ProjectileLaunchMode.Immediate;
            part.projectile.showBeforeAnimationShoot = !spawnOnShoot;
            if (planned != null && planned.HasProjectileSpeed)
            {
                if (part.projectile.settingsOverride == null)
                {
                    part.projectile.settingsOverride = new ProjectileData();
                }

                part.projectile.settingsOverride.speed = Mathf.Max(
                    0.01f,
                    planned.ProjectileSpeedUnreal * UnrealSpeedToUnity);
            }
        }

        return part;
    }

    static void ApplyHomeFlightConfig(
        CompositeHomeProjectileConfig home,
        L2EffectGeneratorFolderBuilder.PlannedFolder planned,
        CompositePartSpawnTiming spawnTiming)
    {
        if (home == null)
        {
            return;
        }

        bool spawnOnShoot = spawnTiming == CompositePartSpawnTiming.OnAnimationShoot;
        home.launchMode = spawnOnShoot
            ? ProjectileLaunchMode.OnAnimationShoot
            : ProjectileLaunchMode.Immediate;
        home.homeAttachmentPoint = EffectAttachmentPoint.CasterCenter;
        home.homeOffset = new Vector3(0f, 0.1f, 0f);
        home.usePathArc = true;
        home.rotateToVelocity = true;
        home.destroyOnArrive = true;
        home.mirrorDualFlight = false;

        if (L2EffectGeneratorAssetOverrides.IsM_u003_bHomeFlight(planned))
        {
            L2EffectGeneratorAssetOverrides.ApplyM_u003_bLiveHomeFlight(home);
            return;
        }

        if (planned != null && planned.HasProjectileSpeed)
        {
            home.speed = Mathf.Max(0.01f, planned.ProjectileSpeedUnreal * UnrealSpeedToUnity);
        }

        if (planned != null && planned.HasProjectileAccSpeed)
        {
            home.acceleration = Mathf.Max(0f, planned.ProjectileAccSpeedUnreal * UnrealSpeedToUnity);
        }
    }

    public static CompositePart CreateV2Part(
        L2EffectGeneratorFolderBuilder.PlannedFolder planned,
        LaunchRow row,
        bool hasProjectileCompanion)
    {
        string suffix = planned != null ? planned.Suffix : null;
        bool isHomeFlight = planned != null &&
            (planned.IsHomeFlight ||
             L2EffectGeneratorAssetOverrides.IsM_u003_bHomeFlight(planned));
        bool isTargetTrailer = planned != null && planned.IsTargetTrailer;
        bool isProjectile = planned != null && planned.IsProjectile && !isHomeFlight;
        string phase = row != null ? row.Phase : null;
        CompositePartSpawnTiming timing = ResolveSpawnTiming(
            phase, suffix, hasProjectileCompanion, planned);
        EffectAttachmentPoint attachment = ResolveAttachment(
            row, suffix, isProjectile, isHomeFlight, timing, planned, phase);
        string bone = null;
        if (row != null &&
            row.HasAttachOn &&
            row.AttachOn == 7 &&
            !string.IsNullOrWhiteSpace(row.Bone))
        {
            bone = row.Bone;
        }

        CompositePart part;
        if (isHomeFlight)
        {
            bool spawnOnShoot = timing == CompositePartSpawnTiming.OnAnimationShoot;
            var home = new HomeFlightPart
            {
                launchMode = spawnOnShoot
                    ? ProjectileLaunchMode.OnAnimationShoot
                    : ProjectileLaunchMode.Immediate,
                homeAttachmentPoint = EffectAttachmentPoint.CasterCenter,
                spawnAttachmentPoint = EffectAttachmentPoint.TargetOverHead,
                homeOffset = new Vector3(0f, 0.1f, 0f),
                usePathArc = true,
                rotateToVelocity = true,
                destroyOnArrive = true
            };
            if (L2EffectGeneratorAssetOverrides.IsM_u003_bHomeFlight(planned))
            {
                L2EffectGeneratorAssetOverrides.ApplyM_u003_bLiveHomeFlight(home);
            }
            else if (planned != null)
            {
                if (planned.HasProjectileSpeed)
                {
                    home.speed = Mathf.Max(
                        0.01f,
                        planned.ProjectileSpeedUnreal * UnrealSpeedToUnity);
                }

                if (planned.HasProjectileAccSpeed)
                {
                    home.acceleration = Mathf.Max(
                        0f,
                        planned.ProjectileAccSpeedUnreal * UnrealSpeedToUnity);
                }
            }

            part = home;
        }
        else if (isProjectile)
        {
            bool spawnOnShoot = timing == CompositePartSpawnTiming.OnAnimationShoot;
            var shot = new ShotProjectilePart
            {
                launchMode = spawnOnShoot
                    ? ProjectileLaunchMode.OnAnimationShoot
                    : ProjectileLaunchMode.Immediate,
                showBeforeAnimationShoot = !spawnOnShoot,
                impactType = ProjectileImpactType.EffectOnly
            };
            if (planned != null && planned.HasProjectileSpeed)
            {
                shot.speed = Mathf.Max(0.01f, planned.ProjectileSpeedUnreal * UnrealSpeedToUnity);
            }

            part = shot;
        }
        else if (isTargetTrailer ||
                 timing == CompositePartSpawnTiming.OnHitCollider ||
                 timing == CompositePartSpawnTiming.OnHitTime ||
                 timing == CompositePartSpawnTiming.OnAnimationShoot ||
                 IsImpactSuffix(suffix) ||
                 IsBlessingBeamPart(planned))
        {
            part = new IndependentEffectPart();
        }
        else
        {
            part = new StationaryPart();
        }

        part.name = planned != null ? planned.FolderName : string.Empty;
        part.inheritRotation = isTargetTrailer;
        part.spawnTiming = timing;
        part.follow = ShouldFollow(row, attachment, timing, isHomeFlight);
        part.scale = row != null && row.HasScale && row.Scale > 0f ? row.Scale : 1f;
        part.spawnAttachmentPoint = attachment;
        part.boneName = bone;
        part.placement = null;
        if (row != null && row.HasSpawnDelay && row.SpawnDelay > 0f)
        {
            part.spawnDelaySeconds = row.SpawnDelay;
        }

        return part;
    }

    public static string DescribeLaunchWarning(LaunchRow row)
    {
        if (row == null || !row.HasAttachOn)
        {
            return null;
        }

        switch (row.AttachOn)
        {
            case 4:
            case 5:
            case 6:
            case 8:
            case 10:
                return "unsupported attach_on=" + row.AttachOn + " (suffix defaults used)";
            case 7:
                return string.IsNullOrWhiteSpace(row.Bone)
                    ? "attach_on=7 without bone name"
                    : null;
            default:
                return null;
        }
    }

    public static bool IsImpactSuffix(string suffix)
    {
        return string.Equals(suffix, "_ta", StringComparison.Ordinal);
    }

    /// <summary>
    /// L2 _ave (arrive / after-visual): beam and burst that start on ShootEvent, not with _ca.
    /// Not listed in skill-effects.tsv; suffix is the launch table.
    /// </summary>
    public static bool IsShootVisualSuffix(string suffix)
    {
        return string.Equals(suffix, "_ave", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsBlessingBeamPart(L2EffectGeneratorFolderBuilder.PlannedFolder planned)
    {
        if (planned == null)
        {
            return false;
        }

        return IsShootVisualSuffix(planned.Suffix) || planned.HasBeamEmitter;
    }

    private static CompositePartSpawnTiming ResolveSpawnTiming(
        string phase,
        string suffix,
        bool hasProjectileCompanion,
        L2EffectGeneratorFolderBuilder.PlannedFolder planned)
    {
        // Table miss only. A real CastingAction row must still win (caster trailers).
        if (string.IsNullOrEmpty(phase) &&
            planned != null &&
            (planned.IsHomeFlight || planned.IsTargetTrailer))
        {
            return CompositePartSpawnTiming.OnAnimationShoot;
        }

        if (string.Equals(phase, "ShotAction", StringComparison.OrdinalIgnoreCase) ||
            IsBlessingBeamPart(planned) ||
            IsShootVisualSuffix(suffix))
        {
            return CompositePartSpawnTiming.OnAnimationShoot;
        }

        if (IsImpactSuffix(suffix))
        {
            return hasProjectileCompanion
                ? CompositePartSpawnTiming.OnHitCollider
                : CompositePartSpawnTiming.OnAnimationShoot;
        }

        if (string.Equals(phase, "ExplosionAction", StringComparison.OrdinalIgnoreCase))
        {
            return hasProjectileCompanion
                ? CompositePartSpawnTiming.OnHitCollider
                : CompositePartSpawnTiming.OnHitTime;
        }

        return CompositePartSpawnTiming.Immediate;
    }

    private static EffectAttachmentPoint ResolveAttachment(
        LaunchRow row,
        string suffix,
        bool isProjectile,
        bool isHomeFlight,
        CompositePartSpawnTiming spawnTiming,
        L2EffectGeneratorFolderBuilder.PlannedFolder planned,
        string phase)
    {
        if (spawnTiming == CompositePartSpawnTiming.OnHitCollider)
        {
            return EffectAttachmentPoint.WorldHitPoint;
        }

        // Home flight (m_u003_b): LocateEffect attach_on=0 spawns at pawn Location
        // (capsule center). Live orb still reads as above the mesh because
        // nameplates sit at Location+CollisionHeight (capsule top). Spawn there.
        if (isHomeFlight)
        {
            return EffectAttachmentPoint.TargetOverHead;
        }

        // PHYS_Trailer / m_u003_c VampireEye: sit at the nameplate (capsule top).
        if (planned != null && planned.IsTargetTrailer)
        {
            return EffectAttachmentPoint.TargetOverHead;
        }

        // _ta without a projectile collider: sit on the skill target (self-cast = caster).
        // WorldHitPoint is only valid after an actual hit event.
        if (IsImpactSuffix(suffix) && spawnTiming != CompositePartSpawnTiming.OnAnimationShoot)
        {
            return spawnTiming == CompositePartSpawnTiming.OnHitCollider
                ? EffectAttachmentPoint.WorldHitPoint
                : EffectAttachmentPoint.TargetCenter;
        }

        if (IsImpactSuffix(suffix) && spawnTiming == CompositePartSpawnTiming.OnAnimationShoot)
        {
            return EffectAttachmentPoint.TargetCenter;
        }

        bool onTarget = row != null && row.OnTarget;
        if (onTarget || IsBlessingBeamPart(planned) || IsShootVisualSuffix(suffix))
        {
            return EffectAttachmentPoint.TargetCenter;
        }

        int attach = row != null && row.HasAttachOn ? row.AttachOn : 0;
        switch (attach)
        {
            case 1:
                return EffectAttachmentPoint.CasterPosition;
            case 2:
                return EffectAttachmentPoint.WeaponSocket;
            case 3:
                return EffectAttachmentPoint.LeftWeaponSocket;
            case 9:
                // attach_on=9 Trail: CastingAction trails the caster; Shot/Explosion
                // trails the hit pawn (m_u003_c VampireEye on target).
                if (string.Equals(phase, "ShotAction", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(phase, "ExplosionAction", StringComparison.OrdinalIgnoreCase))
                {
                    return EffectAttachmentPoint.TargetCenter;
                }

                return string.Equals(suffix, "_ca", StringComparison.Ordinal)
                    ? EffectAttachmentPoint.CasterRoot
                    : EffectAttachmentPoint.CasterCenter;
            default:
                if (isProjectile ||
                    string.Equals(suffix, "_pr", StringComparison.Ordinal) ||
                    string.Equals(suffix, "_fl", StringComparison.Ordinal))
                {
                    return EffectAttachmentPoint.CasterCenter;
                }

                return EffectAttachmentPoint.CasterRoot;
        }
    }

    private static bool ShouldFollow(
        LaunchRow row,
        EffectAttachmentPoint attachment,
        CompositePartSpawnTiming spawnTiming,
        bool isHomeFlight)
    {
        if (isHomeFlight)
        {
            return false;
        }

        if (attachment == EffectAttachmentPoint.WorldHitPoint ||
            attachment == EffectAttachmentPoint.CasterPosition ||
            attachment == EffectAttachmentPoint.TargetPosition)
        {
            return attachment == EffectAttachmentPoint.WorldHitPoint;
        }

        if (row != null && row.HasAttachOn && row.AttachOn == 9)
        {
            return true;
        }

        return spawnTiming != CompositePartSpawnTiming.OnHitCollider;
    }

    private static List<LaunchRow> ParseTsv(string[] lines)
    {
        var rows = new List<LaunchRow>();
        bool headerSeen = false;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            string[] cols = line.Split('\t');
            if (!headerSeen)
            {
                headerSeen = true;
                if (cols.Length > 0 &&
                    cols[0].IndexOf("skill_id", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }
            }

            if (cols.Length < 3)
            {
                continue;
            }

            if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int skillId))
            {
                continue;
            }

            string effectClass = cols[2].Trim();
            if (string.IsNullOrEmpty(effectClass))
            {
                continue;
            }

            var row = new LaunchRow
            {
                SkillId = skillId,
                Phase = cols[1].Trim(),
                EffectClass = effectClass,
                Bone = cols.Length > 4 ? cols[4].Trim() : string.Empty
            };

            if (cols.Length > 3 &&
                int.TryParse(cols[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int attachOn))
            {
                row.HasAttachOn = true;
                row.AttachOn = attachOn;
            }

            if (cols.Length > 5 &&
                TryParseFloat(cols[5], out float spawnDelay))
            {
                row.HasSpawnDelay = true;
                row.SpawnDelay = spawnDelay;
            }

            if (cols.Length > 6 &&
                TryParseFloat(cols[6], out float scale))
            {
                row.HasScale = true;
                row.Scale = scale;
            }

            if (cols.Length > 7)
            {
                string onTarget = cols[7].Trim();
                row.OnTarget = onTarget == "1" ||
                               string.Equals(onTarget, "true", StringComparison.OrdinalIgnoreCase);
            }

            rows.Add(row);
        }

        return rows;
    }

    private static bool TryParseFloat(string value, out float parsed)
    {
        return float.TryParse((value ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed);
    }

    private static string GetFullPath(string assetPath)
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
    }
}
#endif
