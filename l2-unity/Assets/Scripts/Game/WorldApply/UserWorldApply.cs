using UnityEngine;

public sealed class UserWorldApply : EntityWorldApply
{
    private readonly EntityActionMachine _actions;

    public UserWorldApply(EntityActionMachine actions)
    {
        _actions = actions;
    }

    public override void OnMoveTo(Entity entity, Vector3 destination, Vector3 current, CharMoveToLocationDto dto)
    {
        if (entity == null || entity.IsDead())
            return;

        bool holdIdle = EntityActionMachine.ShouldHoldCombatIdle(entity);
        Entity pawn = EntityActionCombatLog.ResolvePawn(entity, destination);
        EntityActionCombatLog.LogCiPawn(entity,
            "MoveTo nick=" + EntityActionCombatLog.NameOf(entity) +
            " action=" + entity.ActionSlot.Action +
            " inCombat=" + entity.InCombat +
            " holdIdle=" + holdIdle +
            " distToDest=" + VectorUtils.Distance2D(entity.transform.position, destination).ToString("F2") +
            " dest=" + EntityActionCombatLog.Vec(destination) +
            " pawn=" + EntityActionCombatLog.Describe(pawn) +
            " distToPawn=" + (pawn != null
                ? VectorUtils.Distance2D(entity.transform.position, pawn.transform.position).ToString("F2")
                : "-") +
            " " + EntityActionCombatLog.ClassifyDest(destination, pawn) +
            (holdIdle ? " SKIP" : " APPLY"));
        if (holdIdle)
        {
            float distToDest = VectorUtils.Distance2D(entity.transform.position, destination);
            if (distToDest <= 1.0f)
            {
                EntityActionCombatLog.LogGap(entity, "MoveTo HOLD_IDLE_SKIP", pawn,
                    " distToDest=" + distToDest.ToString("F2"));
                return;
            }
            EntityActionCombatLog.LogGap(entity, "MoveTo HOLD_IDLE_APPLY", pawn,
                " distToDest=" + distToDest.ToString("F2"));
        }
        EntityActionCombatLog.LogIfWatch(entity,
            "User.MoveTo chase action=" + entity.ActionSlot.Action +
            " inCombat=" + entity.InCombat);
        _actions.Set(entity, EntityActionKind.Move, dto);
    }

    public override void OnStopMove(Entity entity, StopMoveDto dto)
    {
        if (entity != null && entity.IsDead())
            return;
        UserEntity user = entity as UserEntity;
        if (user != null && dto != null)
        {
            float toStop = VectorUtils.Distance2D(entity.transform.position, dto.StopPos);
            float toDest = VectorUtils.Distance2D(entity.transform.position, entity.ActionSlot.Destination);
            Entity pawn = EntityActionCombatLog.PawnOf(entity);
            float pawnDist = entity.ActionSlot.PawnDist;
            float nowToPawn = pawn != null
                ? VectorUtils.Distance2D(entity.transform.position, pawn.transform.position)
                : -1f;
            bool chasing = entity.ActionSlot.Action == EntityActionKind.Move ||
                (entity.Identity != null && MoveAllCharacters.Instance != null &&
                 MoveAllCharacters.Instance.IsMoving(entity.Identity.Id));
            bool skipKeepChase = chasing && pawn != null && pawnDist > 0.01f && nowToPawn > pawnDist + 0.12f;
            bool atDist = pawn != null && pawnDist > 0.01f && nowToPawn >= 0f &&
                nowToPawn <= pawnDist + 0.12f;
            bool skipKeepMove = !skipKeepChase &&
                entity.ActionSlot.Action == EntityActionKind.Move &&
                toStop <= 0.35f &&
                (toDest > 0.5f || atDist) &&
                !(pawn is NpcEntity);
            string skip = skipKeepChase ? " SKIP_KEEP_CHASE" : (skipKeepMove ? " SKIP_KEEP_MOVE" : " APPLY");
            EntityActionCombatLog.LogCiPawn(entity,
                "StopMove nick=" + EntityActionCombatLog.NameOf(entity) +
                " action=" + entity.ActionSlot.Action +
                " inCombat=" + entity.InCombat +
                " toStop=" + toStop.ToString("F2") +
                " toDest=" + toDest.ToString("F2") +
                " pawnDist=" + pawnDist.ToString("F2") +
                " nowToPawn=" + nowToPawn.ToString("F2") +
                " stop=" + EntityActionCombatLog.Vec(dto.StopPos) +
                " dest=" + EntityActionCombatLog.Vec(entity.ActionSlot.Destination) +
                " pawn=" + EntityActionCombatLog.Describe(pawn) +
                " stopToPawn=" + (pawn != null
                    ? VectorUtils.Distance2D(dto.StopPos, pawn.transform.position).ToString("F2")
                    : "-") +
                " destToPawn=" + (pawn != null
                    ? VectorUtils.Distance2D(entity.ActionSlot.Destination, pawn.transform.position).ToString("F2")
                    : "-") +
                " " + EntityActionCombatLog.ClassifyDest(dto.StopPos, pawn) +
                skip);
            CharInfoMoveBudgetLog.Compare(entity, "STOP" + skip, pawn, dto.StopPos, true);
            if (skipKeepChase || skipKeepMove)
                return;
            if (nowToPawn >= 2f)
                EntityActionCombatLog.LogGap(entity, "StopMove APPLY_FAR", pawn,
                    " toStop=" + toStop.ToString("F2") +
                    " stop=" + EntityActionCombatLog.Vec(dto.StopPos) +
                    " stopToPawn=" + (pawn != null
                        ? VectorUtils.Distance2D(dto.StopPos, pawn.transform.position).ToString("F2")
                        : "-"));
        }
        _actions.ApplyStop(entity, dto);
    }

    public override void OnMoveToPawn(Entity entity, MoveToPawnDto dto)
    {
        if (entity == null || entity.IsDead())
            return;
        Entity pawn = IncomingPacketActions.GameWorld != null
            ? IncomingPacketActions.GameWorld.GetEntityNoLockSync(dto.TarObjid)
            : null;
        Vector3 origin = dto.ObjPos;
        Vector3 now = entity.transform.position;
        float distNow = pawn != null ? VectorUtils.Distance2D(now, pawn.transform.position) : -1f;
        float distPkt = pawn != null ? VectorUtils.Distance2D(origin, pawn.transform.position) : -1f;
        float originNow = VectorUtils.Distance2D(origin, now);
        EntityActionCombatLog.LogCiPawn(entity,
            "MoveToPawn nick=" + EntityActionCombatLog.NameOf(entity) +
            " action=" + entity.ActionSlot.Action +
            " inCombat=" + entity.InCombat +
            " distPkt=" + dto.Distance.ToString("F2") +
            " distNow=" + distNow.ToString("F2") +
            " distOriginToPawn=" + distPkt.ToString("F2") +
            " originNow=" + originNow.ToString("F2") +
            " origin=" + EntityActionCombatLog.Vec(origin) +
            " now=" + EntityActionCombatLog.Vec(now) +
            " pawn=" + EntityActionCombatLog.Describe(pawn) +
            " pawnPos=" + (pawn != null ? EntityActionCombatLog.Vec(pawn.transform.position) : "-") +
            " pawnDead=" + (pawn != null && pawn.IsDead()) +
            " pawnMoving=" + EntityActionCombatLog.IsPawnMoving(pawn));
        if (pawn == null || pawn.IsDead())
            EntityActionCombatLog.LogGap(entity, "MoveToPawn PAWN_BAD", pawn,
                " tarId=" + dto.TarObjid +
                " originNow=" + originNow.ToString("F2") +
                " distNow=" + distNow.ToString("F2"));
        else if (originNow >= 2f || (distPkt < 2f && distNow >= 2f))
            EntityActionCombatLog.LogGap(entity, "MoveToPawn ORIGIN_DESYNC", pawn,
                " originNow=" + originNow.ToString("F2") +
                " distPktToPawn=" + distPkt.ToString("F2") +
                " distNow=" + distNow.ToString("F2") +
                " origin=" + EntityActionCombatLog.Vec(origin));
        EntityActionCombatLog.LogIfWatch(entity,
            "User.MoveToPawn chase action=" + entity.ActionSlot.Action +
            " inCombat=" + entity.InCombat);
        _actions.Set(entity, EntityActionKind.Move, dto);
    }

    public override void OnDie(Entity entity, DieDto dto)
    {
        if (entity != null && entity.GetDead())
            return;
        _actions.Die(entity, false);
    }

    public override void OnRevive(Entity entity, ReviveDto dto)
    {
        _actions.Revive(entity);
    }

    public override void OnAttack(Entity attacker, Entity target, AttackDto dto)
    {
        if (attacker == null || attacker.IsDead())
            return;
        if (target != null && target.IsDead())
        {
            EntityActionCombatLog.LogIfWatch(attacker, target,
                "User.OnAttack skip dead target attacker=" + EntityActionCombatLog.NameOf(attacker) +
                " target=" + EntityActionCombatLog.NameOf(target));
            return;
        }
        _actions.Set(attacker, EntityActionKind.Attack, dto);
    }

    public override void OnMagicSkillUse(Entity entity, MagicSkillUseDto dto)
    {
        if (entity == null || entity.IsDead())
            return;
        if (EntityActionSkill.TryApplyWeaponCharge(entity, dto))
            return;
        EntityActionCombatLog.LogCiPawn(entity,
            "MagicSkillUse APPLY CharInfo nick=" + EntityActionCombatLog.NameOf(entity) +
            " skill=" + (dto != null ? dto.SkillId.ToString() : "-") +
            " lvl=" + (dto != null ? dto.SkillLvl.ToString() : "-") +
            " hitTime=" + (dto != null ? dto.HitTime.ToString() : "-"));
        _actions.Set(entity, EntityActionKind.Skill, dto);
    }

    public override void OnAutoAttackStart(Entity entity, AutoAttackStartDto dto)
    {
        _actions.StartAttackStance(entity);
    }

    public override void OnAutoAttackStop(Entity entity, AutoAttackStopDto dto)
    {
        _actions.StopAttack(entity);
    }

    public override void OnSocialAction(Entity entity, SocialActionDto dto)
    {
        _actions.Social(entity, dto);
    }

    public override void OnChangeWaitType(Entity entity, ChangeWaitTypeDto dto)
    {
        _actions.ChangeWaitType(entity, dto);
    }

    public override void OnMagicSkillCanceled(Entity entity, MagicSkillCanceledDto dto)
    {
        if (entity != null && entity.IsDead())
            return;
        if (entity != null && entity.Identity != null)
            CombatFacingService.Instance?.EndFollow(entity.Identity.Id, "skill-canceled");
        _actions.Set(entity, EntityActionKind.Idle, null);
    }
}
