using System;
using UnityEngine;

[Serializable]
public class CompositePrefabPart
{
    public string name;
    public BaseEffect prefab;
    public EffectSettings settingsOverride;
    public EffectAttachmentPoint attachmentPoint = EffectAttachmentPoint.CasterRoot;
    public CompositePartSpawnTiming spawnTiming = CompositePartSpawnTiming.Immediate;
    // Spawns hit-timed part earlier than castData.HitTime (seconds).
    public float hitLeadSeconds = 0f;
    // Extra wait after the resolved spawn event (HF268 spawn_delay).
    public float spawnDelaySeconds = 0f;
    // Optional named bone when attach_on=7. Resolved on the caster Gear before the enum point.
    public string attachmentBoneName;
    // Local offset from resolved attachment point (in attachment transform space if available).
    public Vector3 positionOffset = Vector3.zero;
    // Scales positionOffset by model height to keep visual placement consistent across races.
    public bool normalizeOffsetByOwnerHeight = false;
    public float referenceHeight = 1.8f;
    public float scale = 1f;
    public bool followResolvedTransform = true;
    public bool inheritRotation = true;
    public bool passCastDataToPart = true;
    [Header("Shader Target Position")]
    public bool passShaderTargetPosition = false;
    public EffectAttachmentPoint shaderTargetAttachmentPoint = EffectAttachmentPoint.CasterCenter;
    [Tooltip("Local offset from resolved shader target attachment point (in attachment transform space if available).")]
    public Vector3 shaderTargetPositionOffset = Vector3.zero;
    // If false, part keeps its own prefab/settings lifetime and is not stretched to cast HitTime.
    public bool useCastTimedLifetime = true;
    public bool overrideContinuousLoop = false;
    public bool continuousLoop = false;
    public bool disableShaderLifetime = false;
    public bool overrideHideTime = false;
    public float customHideTime = 1f;
    public bool enableFinalShaderLifetimeOnFade = false;
    public float finalShaderLifetimeMin = 0.15f;
    public float finalShaderLifetimeMax = 0.5f;
    public CompositeProjectileConfig projectile = new CompositeProjectileConfig();
    public CompositeHomeProjectileConfig homeProjectile = new CompositeHomeProjectileConfig();
}
