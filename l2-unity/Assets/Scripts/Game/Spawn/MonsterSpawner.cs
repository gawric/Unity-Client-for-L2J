using UnityEngine;

public sealed class MonsterSpawner
{
    private readonly ObjectPoolManager _pool;

    public MonsterSpawner(ObjectPoolManager pool)
    {
        _pool = pool;
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
        MonsterEntity npc = npcGo.GetComponent<MonsterEntity>();
        npc.Running = npc.Identity != null && npc.Identity.IsRunning;
        npc.NpcData = new NpcData(request.NpcName, request.Npcgrp);
        EntitySpawnShared.ApplyNpcIdentity(npc, request);

        npcGo.transform.name = request.Prefab != null
            ? request.Prefab.name + "_" + identity.Name
            : identity.Name;
        EntitySpawnShared.SanitizeCharacterControllerStepOffset(npcGo);
        npcGo.SetActive(true);
        EntitySpawnShared.ReapplyGroundAfterActivate(npcGo, identity);

        InitMonster(npc, npcGo, world);
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
    }

    private static void InitMonster(Entity npc, GameObject npcGo, IWorldSpawnContext world)
    {
        NetworkAnimationController animationController = npc.GetComponent<NetworkAnimationController>();
        animationController.Initialize();
        EntitySpawnShared.BindGearAndAnimation(npc, npcGo, animationController);
        EntitySpawnShared.RegisterAnimation(world, npc.Identity.Id, animationController, npc);
        EntitySpawnShared.ApplyNpcMoveSpeeds(npc, true);

        if (GravityNpc.Instance != null)
            GravityNpc.Instance.AddGravity(npc.Identity.Id, new GravityData(npc));
    }
}
