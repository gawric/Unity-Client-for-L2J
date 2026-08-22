using UnityEngine;

public sealed class NpcSpawner
{
    private readonly ObjectPoolManager _pool;

    public NpcSpawner(ObjectPoolManager pool)
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

        npcGo.transform.SetParent(world.NpcsContainer);
        NpcEntity npc = npcGo.GetComponent<NpcEntity>();
        npc.NpcData = new NpcData(request.NpcName, request.Npcgrp);
        EntitySpawnShared.ApplyNpcIdentity(npc, request);

        npcGo.transform.name = request.Prefab != null
            ? request.Prefab.name + "_" + identity.Name
            : identity.Name;
        EntitySpawnShared.SanitizeCharacterControllerStepOffset(npcGo);
        npcGo.SetActive(true);
        EntitySpawnShared.ReapplyGroundAfterActivate(npcGo, identity);

        InitNpc(npc, npcGo, world);
        world.RegisterNpc(npc);
        return npc;
    }

    private static void InitNpc(Entity npc, GameObject npcGo, IWorldSpawnContext world)
    {
        NetworkAnimationController animationController = npc.GetComponent<NetworkAnimationController>();
        animationController.Initialize();
        EntitySpawnShared.BindGearAndAnimation(npc, npcGo, animationController);
        EntitySpawnShared.RegisterAnimation(world, npc.Identity.Id, animationController, npc);
        EntitySpawnShared.ApplyNpcMoveSpeeds(npc, false);

        if (npc.name.Equals("Leandro") || npc.name.Equals("Remy"))
        {
            if (GravityNpc.Instance != null)
                GravityNpc.Instance.AddGravity(npc.Identity.Id, new GravityData(npc));
        }
    }
}
