using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC deco: spawn one or more pieces, each with its own attach (bone / feet / overhead).
/// Does not go through EffectManager skill timing.
/// </summary>
public sealed class NpcDecoService
{
    readonly NpcDecoCatalog _catalog;
    readonly Dictionary<int, List<BaseEffect>> _byEntityId = new Dictionary<int, List<BaseEffect>>();
    readonly HashSet<string> _missingLogged = new HashSet<string>();
    readonly List<NpcDecoPiece> _loadBuffer = new List<NpcDecoPiece>(4);
    readonly EffectParticle _hostSettings;

    public NpcDecoService(NpcDecoCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _hostSettings = ScriptableObject.CreateInstance<EffectParticle>();
        _hostSettings.isFollowCaster = true;
        _hostSettings.defaultLifeTime = 3600f;
        _hostSettings.hideTime = 0f;
    }

    public void Start(Entity entity)
    {
        if (entity == null || entity.Identity == null)
            return;

        int entityId = entity.Identity.Id;
        Stop(entityId);

        NpcDecoEffect deco = ResolveDeco(entity);
        if (deco == null || !deco.HasEffectName)
            return;

        if (!_catalog.TryLoadPieces(deco.DecoEffect, _loadBuffer))
        {
            if (_missingLogged.Add(deco.DecoEffect))
            {
                Debug.LogWarning(
                    "NpcDecoService: prefab not found for '" + deco.DecoEffect +
                    "' under Resources/" + NpcDecoCatalog.ResourcesFolder +
                    " (expected " + NpcDecoCatalog.ShortName(deco.DecoEffect) +
                    " or folder of pieces)");
            }
            return;
        }

        List<BaseEffect> spawned = new List<BaseEffect>(_loadBuffer.Count);
        for (int i = 0; i < _loadBuffer.Count; i++)
        {
            BaseEffect instance = SpawnPiece(entity, _loadBuffer[i]);
            if (instance != null)
                spawned.Add(instance);
        }

        if (spawned.Count > 0)
            _byEntityId[entityId] = spawned;
    }

    public void Stop(int entityId)
    {
        if (!_byEntityId.TryGetValue(entityId, out List<BaseEffect> spawned))
            return;

        _byEntityId.Remove(entityId);
        if (spawned == null)
            return;

        for (int i = 0; i < spawned.Count; i++)
        {
            BaseEffect instance = spawned[i];
            if (instance == null)
                continue;

            ParticleEmitterV2.StopAll(instance);
            UnityEngine.Object.Destroy(instance.gameObject);
        }
    }

    public void Stop(Entity entity)
    {
        if (entity != null && entity.Identity != null)
            Stop(entity.Identity.Id);
    }

    BaseEffect SpawnPiece(Entity entity, NpcDecoPiece piece)
    {
        if (piece == null || piece.Prefab == null)
            return null;

        if (!NpcDecoAttachment.TryResolve(entity, piece.Attach, out Transform parent, out Vector3 worldPos))
            return null;

        BaseEffect instance = UnityEngine.Object.Instantiate(
            piece.Prefab,
            worldPos,
            NpcDecoAttachment.UprightYaw(parent),
            parent);
        instance.transform.localScale = Vector3.one;
        if (instance.GetComponent<NpcDecoKeepWorldUp>() == null)
            instance.gameObject.AddComponent<NpcDecoKeepWorldUp>();
        instance.gameObject.name = "NpcDeco_" + piece.Label + "_" + piece.Attach;
        instance.gameObject.SetActive(true);
        instance.BindLifetimeToHost();
        BindHostOwnedEmitters(instance);
        instance.Setup(_hostSettings, null, parent);
        instance.Play();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        NpcDeco2911Trace.DumpSpawnedPrefab(instance);
#endif
        return instance;
    }

    static NpcDecoEffect ResolveDeco(Entity entity)
    {
        Npcgrp npcgrp = null;
        if (entity is NpcEntity npc && npc.NpcData != null)
            npcgrp = npc.NpcData.Npcgrp;
        else if (entity is MonsterEntity monster && monster.NpcData != null)
            npcgrp = monster.NpcData.Npcgrp;

        return npcgrp != null ? npcgrp.Deco : null;
    }

    static void BindHostOwnedEmitters(BaseEffect instance)
    {
        ParticleGroupV2[] groups = instance.GetComponentsInChildren<ParticleGroupV2>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] != null)
                groups[i].BindHostOwnedEmission();
        }
    }
}
