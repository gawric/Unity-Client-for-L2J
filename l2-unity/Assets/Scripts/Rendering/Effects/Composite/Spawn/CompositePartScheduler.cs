using System;
using System.Collections.Generic;
using UnityEngine;

public static class CompositePartScheduler
{
    public static void Queue(
        CompositePrefabPart[] parts,
        MagicCastData castData,
        List<PendingCompositePart> pendingParts,
        List<CompositePrefabPart> pendingHitColliderParts,
        List<CompositePrefabPart> pendingAnimationShootParts,
        Action<CompositePrefabPart> spawnNow)
    {
        pendingParts.Clear();
        pendingHitColliderParts.Clear();
        pendingAnimationShootParts.Clear();

        if (parts == null || spawnNow == null)
        {
            return;
        }

        for (int i = 0; i < parts.Length; i++)
        {
            CompositePrefabPart part = parts[i];
            if (part == null || part.prefab == null)
            {
                continue;
            }

            if (part.spawnTiming == CompositePartSpawnTiming.OnHitCollider)
            {
                pendingHitColliderParts.Add(part);
                continue;
            }

            if (part.spawnTiming == CompositePartSpawnTiming.OnAnimationShoot)
            {
                pendingAnimationShootParts.Add(part);
                continue;
            }

            float delay = CompositeEffectUtilities.ResolveSpawnDelay(part.spawnTiming, castData, part.hitLeadSeconds);
            delay += Mathf.Max(0f, part.spawnDelaySeconds);
            if (delay <= 0f)
            {
                spawnNow(part);
                continue;
            }

            pendingParts.Add(new PendingCompositePart
            {
                Part = part,
                SpawnAtTime = Time.time + delay
            });
        }
    }

    public static bool RequiresHitColliderSpawn(CompositePrefabPart[] parts)
    {
        if (parts == null)
        {
            return false;
        }

        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] != null && parts[i].spawnTiming == CompositePartSpawnTiming.OnHitCollider)
            {
                return true;
            }
        }

        return false;
    }
}
