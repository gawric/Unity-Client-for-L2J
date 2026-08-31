using UnityEngine;

public sealed class MonsterSpawner
{
    private readonly ObjectPoolManager _pool;
    private readonly NpcDecoService _deco;

    public MonsterSpawner(ObjectPoolManager pool, NpcDecoService deco)
    {
        _pool = pool;
        _deco = deco;
    }

    public Entity Spawn(NpcSpawnRequest request, IWorldSpawnContext world)
    {
        if (request == null || world == null)
            return null;

        EntityIdentity identity = request.Identity;
        identity.SetPosY(world.GetGroundHeight(identity.Position));

        GameObject npcGo = EntitySpawnShared.AcquireNpcGameObject(_pool, request.Prefab, identity);
        if (npcGo == null)
            return null;

        npcGo.transform.SetParent(world.MonstersContainer);
        MonsterEntity npc = EntitySpawnShared.FindOnPrefab<MonsterEntity>(npcGo);
        if (npc == null)
        {
            Debug.LogError(
                "MonsterSpawner: MonsterEntity missing on " + npcGo.name +
                " prefab=" + (request.Prefab != null ? request.Prefab.name : "null") +
                " npcId=" + identity.NpcId);
            UnityEngine.Object.Destroy(npcGo);
            return null;
        }
        npc.Running = npc.Identity != null && npc.Identity.IsRunning;
        npc.NpcData = new NpcData(request.NpcName, request.Npcgrp);
        EntitySpawnShared.ApplyNpcIdentity(npc, request);

        npcGo.transform.name = request.Prefab != null
            ? request.Prefab.name + "_" + identity.Name
            : identity.Name;
        EntitySpawnShared.SanitizeCharacterControllerStepOffset(npcGo);
        EntitySpawnShared.DisableLegacyPositionSync(npcGo);
        npcGo.SetActive(true);
        EntitySpawnShared.ReapplyGroundAfterActivate(npcGo, identity);

        InitMonster(npc, npcGo, world);
        _deco.Start(npc);
        CharInfoSpeedLog.LogNpcPacket(npc, "OnNpcInfo spawn");
        world.RegisterNpc(npc);
        return npc;
    }

    public void UpdateInfo(Entity entity, NpcInfoDto npcInfo)
    {
        if (entity == null || !(entity is MonsterEntity))
            return;

        MonsterEntity monster = (MonsterEntity)entity;
        monster.UpdateNpcPAtkSpd((int)npcInfo.Stats.PAtkRealSpeed);
        monster.UpdateNpcRunningSpd(npcInfo.Stats.RunRealSpeed);
        monster.UpdateNpcWalkSpd(npcInfo.Stats.WalkRealSpeed);
        monster.Running = npcInfo.Identity.IsRunning;
        CharInfoSpeedLog.LogNpcPacket(monster, "OnNpcInfo update");
    }

    private static void InitMonster(Entity npc, GameObject npcGo, IWorldSpawnContext world)
    {
        NetworkAnimationController animationController =
            EntitySpawnShared.FindOnPrefab<NetworkAnimationController>(npcGo);
        if (animationController == null)
        {
            Debug.LogError("MonsterSpawner: NetworkAnimationController missing on " + npcGo.name);
            return;
        }
        animationController.Initialize();
        EntitySpawnShared.BindGearAndAnimation(npc, npcGo, animationController);
        EntitySpawnShared.RegisterAnimation(world, npc.Identity.Id, animationController, npc);
        EntitySpawnShared.ApplyNpcMoveSpeeds(npc, true);

        if (GravityNpc.Instance != null)
            GravityNpc.Instance.AddGravity(npc.Identity.Id, new GravityData(npc));
    }
}
