using System.Collections;
using UnityEngine;

public static class EntitySpawnShared
{
    public static bool TryResolveNpc(
        EntityIdentity identity,
        NpcgrpTable npcGrps,
        NpcNameTable npcNames,
        ModelTable models,
        out NpcSpawnRequest request)
    {
        request = null;
        if (identity == null)
            return false;

        npcGrps = npcGrps != null ? npcGrps : NpcgrpTable.Instance;
        npcNames = npcNames != null ? npcNames : NpcNameTable.Instance;
        models = models != null ? models : ModelTable.Instance;

        Npcgrp npcgrp = npcGrps.GetNpcgrp(identity.NpcId);
        NpcName npcName = npcNames.GetNpcName(identity.NpcId);
        if (npcName == null || npcgrp == null)
        {
            Debug.LogError("Npc " + identity.NpcId + " could not be loaded correctly.");
            return false;
        }

        GameObject prefab = models.GetNpc(npcgrp.Mesh);
        if (prefab == null)
        {
            Debug.LogWarning(
                "NPC Not Found Nps!!!!! Need add server ID " + identity.Id + " Npc Id " + identity.NpcId);
            return false;
        }

        identity.EntityType = ResolveEntityType(prefab, npcgrp.ClassName);
        if (identity.NpcId == 31760)
        {
            Debug.Log("SpawnNpc>>> Spawn 31760 p5");
            identity.EntityType = EntityType.NPC;
        }

        request = new NpcSpawnRequest
        {
            Identity = identity,
            Npcgrp = npcgrp,
            NpcName = npcName,
            Prefab = prefab
        };
        return true;
    }

    public static EntityType ResolveEntityType(GameObject prefab, string className)
    {
        bool hasMonster = FindOnPrefab<MonsterEntity>(prefab) != null;
        bool hasNpc = FindOnPrefab<NpcEntity>(prefab) != null;
        if (hasMonster && !hasNpc)
            return EntityType.Monster;
        if (hasNpc && !hasMonster)
            return EntityType.NPC;
        return EntityTypeParser.ParseEntityType(className);
    }

    public static T FindOnPrefab<T>(GameObject go) where T : Component
    {
        if (go == null)
            return null;
        T component = go.GetComponent<T>();
        return component != null ? component : go.GetComponentInChildren<T>(true);
    }

    public static GameObject AcquireNpcGameObject(
        ObjectPoolManager pool,
        GameObject prefab,
        EntityIdentity identity)
    {
        if (prefab == null)
            return null;

        ObjectType? poolType = null;
        if (identity.EntityType == EntityType.NPC)
            poolType = ObjectType.Npc;
        else if (identity.EntityType == EntityType.Monster)
            poolType = ObjectType.Monster;

        Vector3 serverPos = identity.Position;
        Vector3 grounded = GroundSnapHelper.SnapToGroundOrKeep(serverPos);
        identity.Position = grounded;
        if (poolType.HasValue && pool != null)
        {
            ObjectType tag = poolType.Value;
            pool.AddPrefabToPool(tag, prefab, 1);
            GameObject pooled = pool.SpawnFromPool(tag, prefab);
            if (pooled != null)
            {
                ApplyGroundedTransform(pooled, grounded, identity.Heading);
                return pooled;
            }
        }

        bool prefabWasActive = prefab.activeSelf;
        if (prefabWasActive)
            prefab.SetActive(false);

        GameObject npcGo = Object.Instantiate(prefab, grounded, identity.Heading);
        ApplyGroundedTransform(npcGo, grounded, identity.Heading);

        if (prefabWasActive)
            prefab.SetActive(true);

        return npcGo;
    }

    public static void Place(GameObject go, Transform parent, Vector3 position, Quaternion rotation, string name)
    {
        if (go == null)
            return;
        if (parent != null)
            go.transform.SetParent(parent);
        Vector3 grounded = GroundSnapHelper.SnapToGroundOrKeep(position);
        ApplyGroundedTransform(go, grounded, rotation);
        if (!string.IsNullOrEmpty(name))
            go.transform.name = name;
    }

    public static void ReapplyGroundAfterActivate(GameObject go, EntityIdentity identity)
    {
        if (go == null || identity == null)
            return;

        TryApplyGroundSnap(go, identity);

        MonoBehaviour host = go.GetComponent<Entity>();
        if (host == null)
            host = World.Instance;
        if (host != null)
            host.StartCoroutine(RetryGroundSnap(go, identity));
    }

    static IEnumerator RetryGroundSnap(GameObject go, EntityIdentity identity)
    {
        float[] at = { 0.25f, 1f, 2f };
        float elapsed = 0f;
        for (int i = 0; i < at.Length; i++)
        {
            yield return new WaitForSeconds(at[i] - elapsed);
            elapsed = at[i];
            if (go == null || identity == null)
                yield break;

            if (TryApplyGroundSnap(go, identity))
                yield break;
        }
    }

    static bool TryApplyGroundSnap(GameObject go, EntityIdentity identity)
    {
        Vector3 from = go.transform.position;
        if (!GroundSnapHelper.TrySnapToGround(from, out Vector3 snapped))
            return false;

        identity.Position = snapped;
        ApplyGroundedTransform(go, snapped, identity.Heading);
        return true;
    }

    public static void ApplyGroundedTransform(GameObject go, Vector3 position, Quaternion rotation)
    {
        if (go == null)
            return;

        CharacterController[] controllers = go.GetComponentsInChildren<CharacterController>(true);
        bool[] wasEnabled = new bool[controllers.Length];
        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] == null)
                continue;
            wasEnabled[i] = controllers[i].enabled;
            controllers[i].enabled = false;
        }

        go.transform.SetPositionAndRotation(position, rotation);

        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] == null)
                continue;
            controllers[i].enabled = wasEnabled[i];
        }
    }

    public static void SanitizeCharacterControllerStepOffset(GameObject root)
    {
        if (root == null)
            return;

        CharacterController[] controllers = root.GetComponentsInChildren<CharacterController>(true);
        for (int i = 0; i < controllers.Length; i++)
        {
            CharacterController cc = controllers[i];
            if (cc == null)
                continue;
            cc.stepOffset = 0f;
        }
    }

    /// <summary>
    /// Leftover client-prediction: SetNewPosition is never called, so it lerps back to spawn.
    /// </summary>
    public static void DisableLegacyPositionSync(GameObject root)
    {
        if (root == null)
            return;

        NetworkTransformReceive receive = root.GetComponent<NetworkTransformReceive>();
        if (receive != null)
            receive.enabled = false;
    }

    public static void ApplyNpcIdentity(Entity npc, NpcSpawnRequest request)
    {
        EntityIdentity identity = request.Identity;
        Npcgrp npcgrp = request.Npcgrp;
        NpcName npcName = request.NpcName;

        Appearance appearance = new Appearance();
        appearance.RHand = npcgrp.Rhand;
        appearance.LHand = npcgrp.Lhand;
        appearance.CollisionRadius = npcgrp.CollisionRadius;
        appearance.CollisionHeight = npcgrp.CollisionHeight;

        npc.Status = request.Status;
        npc.Stats = request.Stats;
        npc.Identity = identity;
        npc.Identity.NpcClass = npcgrp.ClassName;
        npc.Identity.Name = npcName.Name;
        npc.Identity.Title = npcName.Title;
        if ((npc.Identity.Title == null || npc.Identity.Title.Length == 0) &&
            identity.EntityType == EntityType.Monster)
        {
            npc.Identity.Title = " Lvl: " + npc.Stats.Level;
        }

        npc.Identity.TitleColor = npcName.TitleColor;
        npc.Appearance = appearance;
        npc.SetDead(false);
    }

    public static void BindGearAndAnimation(Entity entity, GameObject go, IAnimationController controller)
    {
        if (go != null)
        {
            Gear gear = go.GetComponent<Gear>();
            if (gear != null)
                gear.Initialize(entity.Identity.Id, entity.RaceId);
        }

        entity.Initialize();
    }

    public static void RegisterAnimation(IWorldSpawnContext world, int objectId, IAnimationController controller, Entity entity)
    {
        if (world != null && world.Animations != null && controller != null)
            world.Animations.RegisterController(objectId, controller, entity);
    }

    public static void ApplyNpcMoveSpeeds(Entity npc, bool useRealPAtk)
    {
        if (useRealPAtk)
            npc.UpdateNpcPAtkSpd((int)npc.Stats.PAtkRealSpeed);
        else
            npc.UpdateNpcPAtkSpd((int)npc.Stats.PAtkSpd);
        npc.UpdateNpcRunningSpd(npc.Stats.RunRealSpeed);
        npc.UpdateNpcWalkSpd(npc.Stats.WalkRealSpeed);
        npc.Running = npc.Identity.IsRunning;
    }
}
