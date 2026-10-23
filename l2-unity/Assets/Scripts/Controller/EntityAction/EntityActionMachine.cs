using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

public sealed class EntityActionMachine : ITickable
{
    public static EntityActionMachine Instance { get; private set; }

    private readonly Dictionary<EntityActionKind, IEntityActionProcess> _processes;
    private readonly L2PawnRange _pawnRange;
    private readonly List<Entity> _active = new List<Entity>();
    private readonly HashSet<int> _activeIds = new HashSet<int>();

    public EntityActionMachine(L2PawnRange pawnRange, L2PawnYaw pawnYaw)
    {
        Instance = this;
        _pawnRange = pawnRange;
        _processes = new Dictionary<EntityActionKind, IEntityActionProcess>
        {
            { EntityActionKind.Idle, new EntityActionIdle() },
            { EntityActionKind.Move, new EntityActionMove(pawnRange, pawnYaw) },
            { EntityActionKind.Stop, new EntityActionStop(pawnRange) },
            { EntityActionKind.Attack, new EntityActionAttack(pawnYaw) },
            { EntityActionKind.Skill, new EntityActionSkill(pawnYaw) }
        };
    }

    public static bool IsAllowed(Entity entity)
    {
        return entity != null && !(entity is PlayerEntity);
    }

    public static bool ShouldHoldCombatIdle(Entity entity)
    {
        if (entity == null || !entity.InCombat)
            return false;
        if (entity.ActionSlot.Action == EntityActionKind.Move)
            return false;
        return entity.ActionSlot.Target == null && entity.AttackTarget == null;
    }

    public void Set(Entity entity, EntityActionKind action, object payload)
    {
        MagicSkillUseDto windMagic = payload as MagicSkillUseDto;
        bool windstrike = windMagic != null && windMagic.SkillId == 4247 &&
            action == EntityActionKind.Skill;
        if (!IsAllowed(entity))
        {
            if (windstrike)
            {
                Debug.Log(
                    "[WINDSTRIKE] Machine.Set blocked type=" +
                    (entity != null ? entity.GetType().Name : "null"));
            }
            return;
        }

        EntityActionSlot slot = entity.ActionSlot;
        if (windstrike)
        {
            Debug.Log(
                "[WINDSTRIKE] Machine.Set from=" + slot.Action +
                " to=Skill");
        }
        // if (NpcCombatTrace.IsCombatNpc(entity))
            // NpcCombatTrace.Write("ACTION", entity, null,
                // "from=" + slot.Action + " to=" + action + " " + NpcCombatTrace.Place(entity));
        // EntityActionCombatLog.RememberBeforeSet(slot.Action);
        slot.Write(action, payload, slot.Target, slot.Destination);

        IEntityActionProcess process;
        if (_processes.TryGetValue(action, out process))
        {
            if (windstrike)
                Debug.Log("[WINDSTRIKE] Machine.Enter " + process.GetType().Name);
            process.Enter(entity, payload);
            if (windstrike)
                Debug.Log("[WINDSTRIKE] Machine.Enter returned");
        }
        else if (windstrike)
        {
            Debug.Log("[WINDSTRIKE] Machine.Set no process for " + action);
        }

        Track(entity, action);
    }

    public void Die(Entity entity, bool alreadyCorpse = false, string source = null)
    {
        if (entity == null)
            return;

        string src = string.IsNullOrEmpty(source) ? "machine" : source;

        // EntityActionCombatLog.LogIfWatch(entity,
            // "Die name=" + EntityActionCombatLog.NameOf(entity) +
            // " allowed=" + IsAllowed(entity) +
            // " alreadyCorpse=" + alreadyCorpse +
            // " src=" + src +
            // " inCombat=" + entity.InCombat +
            // " action=" + entity.ActionSlot.Action);

        if (IsAllowed(entity))
        {
            entity.InCombat = false;
            entity.AttackTarget = null;
            if (entity.ActionSlot != null)
                entity.ActionSlot.Target = null;
            EntityActionVisual.CancelMove(entity);
            EntityActionVisual.PlayDeath(entity, alreadyCorpse);
            Set(entity, EntityActionKind.Stop, null);
        }

        StopAttackersOf(entity);
    }

    public void CancelAttackKeepStance(Entity entity)
    {
        if (!IsAllowed(entity) || entity.IsDead())
            return;

        if (HitManager.Instance != null)
            HitManager.Instance.FlushRemoteMeleeHit(entity);

        EntityActionKind action = entity.ActionSlot.Action;
        entity.InCombat = true;
        entity.AttackTarget = null;
        entity.ActionSlot.Target = null;
        L2PawnRange.ClearIgnoredPawn(entity);
        EntityActionVisual.CancelMove(entity);

        // EntityActionCombatLog.Watch(entity);
        // EntityActionCombatLog.LogIfWatch(entity,
            // "ReleaseDeadTarget keepSwing=" + IsFinishingSwing(entity) +
            // " " + EntityActionCombatLog.Describe(entity) +
            // " fromAction=" + action);
        // EntityActionCombatLog.LogGap(entity, "ReleaseDeadTarget", null,
            // " fromAction=" + action +
            // " keepSwing=" + IsFinishingSwing(entity));

        if (IsFinishingSwing(entity) || action == EntityActionKind.Idle)
            return;

        Set(entity, EntityActionKind.Idle, null);
    }

    public void ApplyStop(Entity entity, StopMoveDto dto, bool skipPositionCorrection = false)
    {
        if (!IsAllowed(entity) || entity.IsDead())
            return;

        EntityActionKind action = entity.ActionSlot.Action;
        bool preserveCombat = IsSkillOrAttack(action) || IsFinishingSwing(entity);

        EntityActionStop.StopLocomotion(entity);
        EntityActionStop.TryCorrectPosition(entity, dto, _pawnRange, skipPositionCorrection || preserveCombat);

        if (preserveCombat)
        {
            // EntityActionCombatLog.LogStep(entity,
                // "STOP KEEP_COMBAT nick=" + EntityActionCombatLog.NameOf(entity) +
                // " action=" + action +
                // " skipPos=" + skipPositionCorrection +
                // " nowL2=" + EntityActionCombatLog.L2(entity.transform.position));
            // EntityActionCombatLog.LogIfWatch(entity,
                // "StopMove keep combat name=" + EntityActionCombatLog.NameOf(entity) +
                // " action=" + action);
            return;
        }

        // EntityActionCombatLog.LogIfWatch(entity,
            // "StopMove→Stop name=" + EntityActionCombatLog.NameOf(entity) +
            // " action=" + action +
            // " inCombat=" + entity.InCombat);
        // EntityActionCombatLog.LogCiPawn(entity,
            // "ApplyStop SET Stop nick=" + EntityActionCombatLog.NameOf(entity) +
            // " action=" + action +
            // " inCombat=" + entity.InCombat +
            // " skipPos=" + skipPositionCorrection);
        Set(entity, EntityActionKind.Stop, dto);
    }

    void StopAttackersOf(Entity dead)
    {
        if (dead == null)
            return;

        List<Entity> attackers = new List<Entity>();
        CollectAttackersOf(dead, _active, attackers);

        World world = IncomingPacketActions.GameWorld != null
            ? IncomingPacketActions.GameWorld
            : World.Instance;
        if (world != null)
            world.ForEachEntity(entity => CollectAttacker(dead, entity, attackers));

        // if (EntityActionCombatLog.IsWatch(dead) || EntityActionCombatLog.ContainsWatch(attackers))
        {
            string names = "";
            for (int i = 0; i < attackers.Count; i++)
            {
                if (i > 0)
                    names += ", ";
                // names += EntityActionCombatLog.Describe(attackers[i]) +
                    // "(action=" + attackers[i].ActionSlot.Action +
                    // " inCombat=" + attackers[i].InCombat + ")";
            }
            // EntityActionCombatLog.Log(
                // "StopAttackersOf dead=" + EntityActionCombatLog.NameOf(dead) +
                // " count=" + attackers.Count +
                // " active=" + _active.Count +
                // " attackers=[" + names + "]");
        }

        for (int i = 0; i < attackers.Count; i++)
            CancelAttackKeepStance(attackers[i]);
    }

    static void CollectAttackersOf(Entity dead, List<Entity> source, List<Entity> attackers)
    {
        if (source == null)
            return;
        for (int i = 0; i < source.Count; i++)
            CollectAttacker(dead, source[i], attackers);
    }

    static void CollectAttacker(Entity dead, Entity attacker, List<Entity> attackers)
    {
        if (attacker == null || attacker == dead || attacker.IsDead())
            return;
        if (!IsTargeting(attacker, dead))
            return;
        if (!attackers.Contains(attacker))
            attackers.Add(attacker);
    }

    static bool IsTargeting(Entity attacker, Entity dead)
    {
        if (attacker.ActionSlot.Target == dead)
            return true;
        if (attacker.AttackTarget != null && dead != null && dead.transform != null)
            return attacker.AttackTarget == dead.transform;
        return false;
    }

    public void Revive(Entity entity)
    {
        if (!IsAllowed(entity))
            return;

        entity.SetDead(false);
        if (entity.Status != null && entity.Status.GetHp() <= 0)
        {
            float maxHp = entity.Stats != null && entity.Stats.MaxHp > 0 ? entity.Stats.MaxHp : 100f;
            entity.Status.SetHp(maxHp);
        }
        EntityActionVisual.CancelMove(entity);
        Set(entity, EntityActionKind.Attack, new ReviveDto());
    }

    public void StopAttack(Entity entity)
    {
        if (!IsAllowed(entity) || entity.IsDead())
            return;

        entity.InCombat = false;
        RefreshStandWaitIfIdle(entity, "AutoAttackStop");
    }

    public void StartAttackStance(Entity entity)
    {
        if (!IsAllowed(entity) || entity.IsDead())
            return;

        entity.InCombat = true;
        RefreshStandWaitIfIdle(entity, "AutoAttackStart");
    }

    void RefreshStandWaitIfIdle(Entity entity, string reason)
    {
        EntityActionKind action = entity.ActionSlot.Action;
        bool busy = action == EntityActionKind.Move ||
            IsSkillOrAttack(action) ||
            IsFinishingSwing(entity);
        if (busy)
        {
            // EntityActionCombatLog.LogIfWatch(entity,
                // reason + " FLAG_ONLY name=" + EntityActionCombatLog.NameOf(entity) +
                // " action=" + action);
            // EntityActionCombatLog.LogCiPawn(entity,
                // reason + " FLAG_ONLY nick=" + EntityActionCombatLog.NameOf(entity) +
                // " action=" + action +
                // " inCombat=" + entity.InCombat +
                // " pawn=" + EntityActionCombatLog.Describe(EntityPawnLookup.PawnOf(entity)));
            return;
        }

        // EntityActionCombatLog.LogIfWatch(entity,
            // reason + " REFRESH name=" + EntityActionCombatLog.NameOf(entity) +
            // " inCombat=" + entity.InCombat);
        // EntityActionCombatLog.LogCiPawn(entity,
            // reason + " REFRESH nick=" + EntityActionCombatLog.NameOf(entity) +
            // " action=" + action +
            // " inCombat=" + entity.InCombat);
        EntityActionVisual.PlayStandWait(entity);
    }

    public void Social(Entity entity, SocialActionDto dto)
    {
    }

    public void ChangeWaitType(Entity entity, ChangeWaitTypeDto dto)
    {
        if (!IsAllowed(entity) || dto == null)
            return;
        if (entity.IsDead() && dto.WaitType != WaitType.WT_STOP_FAKEDEATH)
            return;

        EntityActionVisual.CancelMove(entity);
        EntityActionVisual.PlayWaitType(entity, dto.WaitType);
        if (dto.WaitType == WaitType.WT_STANDING || dto.WaitType == WaitType.WT_STOP_FAKEDEATH)
            Set(entity, EntityActionKind.Idle, null);
        else
        {
            entity.ActionSlot.Write(EntityActionKind.Stop, dto, entity.ActionSlot.Target, entity.ActionSlot.Destination);
            Track(entity, EntityActionKind.Stop);
        }
    }

    public void NotifyArrived(Entity entity)
    {
        if (!IsAllowed(entity) || entity.IsDead())
            return;
        if (IsSkillOrAttack(entity.ActionSlot.Action) || ShouldHoldCombatIdle(entity))
        {
            EntitySpawnShared.LogStriderGait("arrived-ignore", entity, "reason=combat");
            return;
        }

        Entity pawn = EntityPawnLookup.PawnOf(entity);
        if (entity is UserEntity && pawn != null && entity.ActionSlot.PawnDist > 0.01f)
        {
            MagicGaitTrace.Event(entity, "NotifyArrived-pawn",
                "pawnDist=" + entity.ActionSlot.PawnDist.ToString("F2") +
                " toPawn=" + VectorUtils.Distance2D(
                    entity.transform.position, pawn.transform.position).ToString("F2") +
                " → CancelMove+PlayStandWait");
            EntityActionVisual.CancelMove(entity);
            EntityActionVisual.PlayStandWait(entity);
            string pose = entity.InCombat ? "Arrived ATKWAIT" : "Arrived WAIT";
            // EntityActionCombatLog.MarkArrivedPose(pose);
            // EntityActionCombatLog.LogCiPawn(entity,
                // pose +
                // " nick=" + EntityActionCombatLog.NameOf(entity) +
                // " nowToPawn=" + VectorUtils.Distance2D(entity.transform.position, pawn.transform.position).ToString("F2") +
                // " pawnDist=" + entity.ActionSlot.PawnDist.ToString("F2") +
                // " inCombat=" + entity.InCombat +
                // " pawn=" + EntityActionCombatLog.Describe(pawn) +
                // EntityActionCombatLog.ChaseDump(entity, pawn));
            float arrivedToPawn = VectorUtils.Distance2D(entity.transform.position, pawn.transform.position);
            // if (arrivedToPawn >= 2f)
                // EntityActionCombatLog.LogGap(entity, "Arrived KEEP_RUN_FAR", pawn);
            // CharInfoSpeedLog.LogArrive(entity, pose);
            return;
        }

        // EntityActionCombatLog.LogCiPawn(entity,
            // "Arrived→Idle nick=" + EntityActionCombatLog.NameOf(entity) +
            // " now=" + EntityActionCombatLog.Vec(entity.transform.position) +
            // " dest=" + EntityActionCombatLog.Vec(entity.ActionSlot.Destination) +
            // " pawn=" + EntityActionCombatLog.Describe(pawn) +
            // " nowToPawn=" + (pawn != null
                // ? VectorUtils.Distance2D(entity.transform.position, pawn.transform.position).ToString("F2")
                // : "-") +
            // " destToPawn=" + (pawn != null
                // ? VectorUtils.Distance2D(entity.ActionSlot.Destination, pawn.transform.position).ToString("F2")
                // : "-") +
            // " " + EntityActionCombatLog.ClassifyDest(entity.transform.position, pawn));
        Set(entity, EntityActionKind.Idle, null);
    }

    public void Remove(Entity entity)
    {
        if (entity == null || entity.Identity == null)
            return;

        L2PawnRange.ClearIgnoredPawn(entity);
        _activeIds.Remove(entity.Identity.Id);
        _active.Remove(entity);
        entity.ActionSlot.Clear();
    }

    public void Tick()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            Entity entity = _active[i];
            if (entity == null)
            {
                _active.RemoveAt(i);
                continue;
            }

            IEntityActionProcess process;
            if (_processes.TryGetValue(entity.ActionSlot.Action, out process))
                process.Tick(entity);
        }
    }

    public static bool IsFinishingSwing(Entity entity)
    {
        UserEntity user = entity as UserEntity;
        return user != null && user.IsAttackVisualPlaying();
    }

    public static bool IsSkillOrAttack(EntityActionKind action)
    {
        return action == EntityActionKind.Attack || action == EntityActionKind.Skill;
    }

    void Track(Entity entity, EntityActionKind action)
    {
        if (entity.Identity == null)
            return;

        int id = entity.Identity.Id;
        if (action == EntityActionKind.Idle || action == EntityActionKind.Stop)
        {
            if (_activeIds.Remove(id))
                _active.Remove(entity);
            return;
        }

        if (_activeIds.Add(id))
            _active.Add(entity);
    }
}

