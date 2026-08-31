using UnityEngine;

public sealed class NpcSpawner
{
    private readonly ObjectPoolManager _pool;
    private readonly AppearFadeService _appearFade;
    private readonly NpcDecoService _deco;

    public NpcSpawner(ObjectPoolManager pool, AppearFadeService appearFade, NpcDecoService deco)
    {
        _pool = pool;
        _appearFade = appearFade;
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

        npcGo.transform.SetParent(world.NpcsContainer);
        NpcEntity npc = EntitySpawnShared.FindOnPrefab<NpcEntity>(npcGo);
        if (npc == null)
        {
            Debug.LogError(
                "NpcSpawner: NpcEntity missing on " + npcGo.name +
                " prefab=" + (request.Prefab != null ? request.Prefab.name : "null") +
                " npcId=" + identity.NpcId);
            UnityEngine.Object.Destroy(npcGo);
            return null;
        }
        npc.NpcData = new NpcData(request.NpcName, request.Npcgrp);
        EntitySpawnShared.ApplyNpcIdentity(npc, request);

        npcGo.transform.name = request.Prefab != null
            ? request.Prefab.name + "_" + identity.Name
            : identity.Name;
        EntitySpawnShared.SanitizeCharacterControllerStepOffset(npcGo);
        EntitySpawnShared.DisableLegacyPositionSync(npcGo);
        npcGo.SetActive(true);
        EntitySpawnShared.ReapplyGroundAfterActivate(npcGo, identity);

        InitNpc(npc, npcGo, world);
        _deco.Start(npc);
        world.RegisterNpc(npc);
        if (_appearFade != null && !npc.IsDead())
            _appearFade.Begin(npc);
        return npc;
    }

    private static void InitNpc(Entity npc, GameObject npcGo, IWorldSpawnContext world)
    {
        NetworkAnimationController animationController =
            EntitySpawnShared.FindOnPrefab<NetworkAnimationController>(npcGo);
        if (animationController == null)
        {
            Debug.LogError("NpcSpawner: NetworkAnimationController missing on " + npcGo.name);
            return;
        }
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
