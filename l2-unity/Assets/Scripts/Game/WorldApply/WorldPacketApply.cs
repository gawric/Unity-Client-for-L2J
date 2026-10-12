using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class WorldPacketApply
{
    private readonly Dictionary<Type, EntityWorldApply> _byType = new Dictionary<Type, EntityWorldApply>();
    private readonly World _world;
    private readonly HtmlWindow _html;
    private readonly PlayerPositionSender _positionSender;

    public WorldPacketApply(
        World world,
        HtmlWindow html,
        PlayerPositionSender positionSender,
        PlayerWorldApply playerApply,
        NpcWorldApply npcApply,
        MonsterWorldApply monsterApply,
        UserWorldApply userApply)
    {
        _world = world;
        _html = html;
        _positionSender = positionSender;
        Register<PlayerEntity>(playerApply);
        Register<NpcEntity>(npcApply);
        Register<MonsterEntity>(monsterApply);
        Register<UserEntity>(userApply);
    }

    public void Register<TEntity>(EntityWorldApply apply) where TEntity : Entity
    {
        _byType[typeof(TEntity)] = apply;
    }

    public void UpdateNpc(NpcInfoDto npcInfo)
    {
        if (_world == null || npcInfo == null || npcInfo.Identity == null)
            return;
        if (!IncomingPacketActions.IsWorldSpawnReady())
            return;

        Entity entity = _world.GetEntityNoLockSync(npcInfo.Identity.Id);
        if (entity == null)
            _world.SpawnNpc(npcInfo.Identity, npcInfo.Status, npcInfo.Stats);
        else
            _world.UpdateNpc(entity, npcInfo);
    }

    public void FlushKnownlist()
    {
        StorageNpc storage = StorageNpc.getInstance();
        NpcInfoDto[] npcs = storage.CopyNpcs();
        CharInfoDto[] chars = storage.CopyChars();
        Debug.Log("[WorldSpawn] FlushKnownlist npcs=" + npcs.Length + " chars=" + chars.Length);
        for (int i = 0; i < npcs.Length; i++)
            UpdateNpc(npcs[i]);
        for (int i = 0; i < chars.Length; i++)
            UpdateUser(chars[i]);
    }

    public void UpdateUser(CharInfoDto info)
    {
        if (_world == null || info == null || info.Identity == null)
        {
            GearFlowLog.Warn("UpdateUser abort world/info null");
            return;
        }
        if (!IncomingPacketActions.IsWorldSpawnReady())
        {
            GearFlowLog.Warn("UpdateUser abort world not ready id=" + info.Identity.Id +
                " nick=" + info.Identity.Name + " " + GearFlowLog.Paperdoll(info.Appearance));
            return;
        }

        PlayerEntity local = PlayerEntity.Instance;
        if (local != null && local.Identity != null && local.Identity.Id == info.Identity.Id)
        {
            GearFlowLog.Info("UpdateUser SKIP local id=" + info.Identity.Id);
            return;
        }

        Entity entity = _world.GetEntityNoLockSync(info.Identity.Id);
        if (entity == null)
        {
            GearFlowLog.Info("UpdateUser SPAWN id=" + info.Identity.Id +
                " nick=" + info.Identity.Name + " " + GearFlowLog.Paperdoll(info.Appearance));
            _world.SpawnUser(info);
            return;
        }

        if (entity is UserEntity)
        {
            GearFlowLog.Info("UpdateUser APPLY UserEntity id=" + info.Identity.Id +
                " nick=" + info.Identity.Name +
                " type=" + entity.GetType().Name +
                " have " + GearFlowLog.Paperdoll(entity) +
                " want " + GearFlowLog.Paperdoll(info.Appearance));
            _world.UpdateUser(entity, info);
            return;
        }

        GearFlowLog.Warn("UpdateUser SKIP not UserEntity id=" + info.Identity.Id +
            " type=" + entity.GetType().Name);
    }

    public void MoveTo(int objId, Vector3 destination, Vector3 current, CharMoveToLocationDto dto)
    {
        Entity entity = GetEntity(objId);
        if (entity == null || string.Equals(entity.name, "Elpy"))
            return;

        if (NpcCombatTrace.IsCombatNpc(entity))
            NpcCombatTrace.Write(entity.IsDead() ? "MOVE_SKIP" : "MOVE", entity, null,
                NpcCombatTrace.Place(entity) +
                " dest=" + destination.x.ToString("F0") + "," + destination.y.ToString("F0") + "," + destination.z.ToString("F0"));

        EntityWorldApply apply;
        if (TryResolve(entity, out apply))
            apply.OnMoveTo(entity, destination, current, dto);
        UnityReplaySession.OnMoveTo(entity, destination);
    }

    public void StopMove(StopMoveDto dto)
    {
        if (dto == null)
            return;

        Entity entity = GetEntity(dto.ObjId);
        EntityWorldApply apply;
        if (entity == null || !TryResolve(entity, out apply))
            return;

        if (NpcCombatTrace.IsCombatNpc(entity))
            NpcCombatTrace.Write("STOP", entity, null, NpcCombatTrace.Place(entity));

        ReplayAttackTrace.LogStop(entity, dto);
        apply.OnStopMove(entity, dto);
        UnityChainLog.OnStopMove(entity, dto);
        UnityReplaySession.OnStopMove(entity, dto);
    }

    public void MoveToPawn(MoveToPawnDto dto)
    {
        Entity entity = GetEntity(dto.ObjId);
        EntityWorldApply apply;
        if (entity == null || !TryResolve(entity, out apply))
            return;

        if (NpcCombatTrace.IsCombatNpc(entity))
        {
            Entity pawn = GetEntity(dto.TarObjid);
            NpcCombatTrace.Write(entity.IsDead() ? "PAWN_SKIP" : "PAWN", entity, pawn,
                NpcCombatTrace.Place(entity) + " pawnId=" + dto.TarObjid + " distUu=" + dto.DistanceUu);
        }

        ReplayAttackTrace.LogPawn(entity, GetEntity(dto.TarObjid), dto.DistanceUu);
        apply.OnMoveToPawn(entity, dto);
        UnityChainLog.OnMoveToPawn(entity, dto);
        UnityReplaySession.OnMoveToPawn(entity, dto);
    }

    public void Die(DieDto dto)
    {
        Entity entity = GetEntity(dto.ObjectId);
        ReplayAttackTrace.Write("DIE_PACKET", entity, null, "objectId=" + dto.ObjectId);
        EntityWorldApply apply;
        if (entity == null || !TryResolve(entity, out apply))
            return;

        EntityActionCombatLog.LogIfWatch(entity,
            "packet Die name=" + EntityActionCombatLog.NameOf(entity) +
            " id=" + dto.ObjectId +
            " dead=" + entity.IsDead() +
            " type=" + entity.GetType().Name);
        apply.OnDie(entity, dto);
        UnityChainLog.OnDie(entity, dto);
        UnityReplaySession.OnDie(entity);
    }

    public void Revive(ReviveDto dto)
    {
        if (dto == null)
            return;

        Entity entity = GetEntity(dto.ObjectId);
        EntityWorldApply apply;
        if (entity == null || !TryResolve(entity, out apply))
            return;

        apply.OnRevive(entity, dto);
    }

    public void Attack(AttackDto dto)
    {
        if (_world == null)
            return;

        Entity target = _world.GetEntityNoLockSync(dto.TargetId);
        Entity attacker = _world.GetEntityNoLockSync(dto.AttackerObjId);
        if (NpcCombatTrace.IsCombatNpc(attacker))
            NpcCombatTrace.Write(attacker.IsDead() || target == null || target.IsDead() ? "ATTACK_SKIP" : "ATTACK",
                attacker, target,
                NpcCombatTrace.Place(attacker) + " damage=" + dto.Damage + " flags=" + dto.Flags);
        ReplayAttackTrace.Write(
            "ATTACK_PACKET",
            attacker,
            target,
            "damage=" + dto.Damage + " flags=" + dto.Flags +
            " attackerId=" + dto.AttackerObjId + " targetId=" + dto.TargetId);
        ReplayAttackTrace.LogHit(attacker, target, dto);
        EntityActionCombatLog.LogIfWatch(attacker, target,
            "packet Attack attacker=" + EntityActionCombatLog.Describe(attacker) +
            " target=" + EntityActionCombatLog.Describe(target) +
            " attackerDead=" + (attacker != null && attacker.IsDead()) +
            " targetDead=" + (target != null && target.IsDead()) +
            " attackerAction=" + (attacker != null ? attacker.ActionSlot.Action.ToString() : "none"));
        EntityWorldApply apply;
        if (attacker == null || !TryResolve(attacker, out apply))
            return;

        UnityReplaySession.OnAttack(attacker, dto);
        apply.OnAttack(attacker, target, dto);
        UnityChainLog.OnAttack(attacker, target, dto);
    }

    public void AutoAttackStart(AutoAttackStartDto dto)
    {
        if (dto == null)
            return;
        Entity entity = GetEntity(dto.EntityId);
        EntityWorldApply apply;
        if (entity == null || !TryResolve(entity, out apply))
            return;
        if (NpcCombatTrace.IsCombatNpc(entity))
            NpcCombatTrace.Write("ATK_START", entity, null, NpcCombatTrace.Place(entity));
        EntityActionCombatLog.LogIfWatch(entity,
            "packet AutoAttackStart name=" + EntityActionCombatLog.NameOf(entity) +
            " id=" + dto.EntityId);
        apply.OnAutoAttackStart(entity, dto);
        UnityChainLog.OnAutoAttackStart(entity, dto);
        UnityReplaySession.OnAttackStart(entity);
    }

    public void AutoAttackStop(AutoAttackStopDto dto)
    {
        if (dto == null)
            return;
        Entity entity = GetEntity(dto.EntityId);
        EntityWorldApply apply;
        if (entity == null || !TryResolve(entity, out apply))
            return;
        if (NpcCombatTrace.IsCombatNpc(entity))
            NpcCombatTrace.Write("ATK_STOP", entity, null, NpcCombatTrace.Place(entity));
        EntityActionCombatLog.LogIfWatch(entity,
            "packet AutoAttackStop name=" + EntityActionCombatLog.NameOf(entity) +
            " id=" + dto.EntityId +
            " inCombat=" + entity.InCombat);
        apply.OnAutoAttackStop(entity, dto);
        UnityChainLog.OnAutoAttackStop(entity, dto);
        UnityReplaySession.OnAttackStop(entity);
    }

    public void SocialAction(SocialActionDto dto)
    {
        if (dto == null)
            return;
        Entity entity = GetEntity(dto.ObjectId);
        EntityActionCombatLog.LogIfWatch(entity,
            "packet Social IGNORE name=" + EntityActionCombatLog.NameOf(entity) +
            " id=" + dto.ObjectId +
            " actionId=" + dto.ActionId);
    }

    public void ChangeWaitType(ChangeWaitTypeDto dto)
    {
        if (dto == null)
            return;
        Entity entity = GetEntity(dto.ObjectId);
        EntityWorldApply apply;
        if (entity == null || !TryResolve(entity, out apply))
            return;
        apply.OnChangeWaitType(entity, dto);
    }

    public void MagicSkillCanceled(MagicSkillCanceledDto dto)
    {
        if (dto == null)
            return;
        Entity entity = GetEntity(dto.ObjectId);
        EntityWorldApply apply;
        if (entity == null || !TryResolve(entity, out apply))
            return;
        apply.OnMagicSkillCanceled(entity, dto);
    }

    public void MagicSkillUse(MagicSkillUseDto dto)
    {
        if (dto == null)
            return;

        Entity entity = GetEntity(dto.AttackerObjId);
        if (entity == null)
            entity = PlayerEntity.Instance;

        EntityWorldApply apply;
        if (entity == null || !TryResolve(entity, out apply))
            return;

        apply.OnMagicSkillUse(entity, dto);
        UnityReplaySession.OnMagicSkillUse(entity, dto);
    }

    public void MagicSkillLaunched(MagicSkillLaunchedDto dto)
    {
        if (dto == null)
            return;
        Entity entity = GetEntity(dto.ObjectId);
        if (entity == null)
            entity = PlayerEntity.Instance;
        if (entity == null)
            return;
        UnityReplaySession.OnMagicLaunched(entity, dto);
    }

    public void ShowNpcHtml(NpcHtmlMessageDto dto)
    {
        if (dto.GetNpcId() == 0 && _html != null)
        {
            _html.InjectToWindow(dto.Html);
            _html.ShowWindowToCenterAndBringToFront();
        }

        Entity npc = GetEntity(dto.GetNpcId());
        EntityWorldApply apply;
        if (npc == null || !TryResolve(npc, out apply))
            return;

        apply.OnNpcHtml(npc, dto);
    }

    public void SendArrivedPosition()
    {
        PlayerController player = PlayerController.Instance;
        if (player == null)
            return;

        _positionSender.SendServerArrivedPosition(player.transform.position);
    }

    private Entity GetEntity(int objId)
    {
        return _world == null ? null : _world.GetEntityNoLockSync(objId);
    }

    private bool TryResolve(Entity entity, out EntityWorldApply apply)
    {
        Type type = entity.GetType();
        while (type != null && type != typeof(Entity) && type != typeof(object))
        {
            if (_byType.TryGetValue(type, out apply))
                return true;
            type = type.BaseType;
        }

        apply = null;
        return false;
    }
}
