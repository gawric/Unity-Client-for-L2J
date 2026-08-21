using UnityEngine;

public sealed class MonsterWorldApply : EntityWorldApply
{
    private readonly EntityActionMachine _actions;

    public MonsterWorldApply(EntityActionMachine actions)
    {
        _actions = actions;
    }

    public override void OnMoveTo(Entity entity, Vector3 destination, Vector3 current, CharMoveToLocationDto dto)
    {
        if (entity == null || entity.IsDead())
            return;
        EntityActionCombatLog.LogIfWatch(entity,
            "Monster.MoveTo chase action=" + entity.ActionSlot.Action +
            " inCombat=" + entity.InCombat);
        DebugLineDraw.ShowDrawLineDebug(dto.ObjId, destination, current, Color.red);
        _actions.Set(entity, EntityActionKind.Move, destination);
    }

    public override void OnStopMove(Entity entity, StopMoveDto dto)
    {
        _actions.ApplyStop(entity, dto);
    }

    public override void OnMoveToPawn(Entity entity, MoveToPawnDto dto)
    {
        if (entity == null || entity.IsDead())
            return;
        EntityActionCombatLog.LogIfWatch(entity,
            "Monster.MoveToPawn chase action=" + entity.ActionSlot.Action +
            " inCombat=" + entity.InCombat);
        _actions.Set(entity, EntityActionKind.Move, dto);
    }

    public override void OnDie(Entity entity, DieDto dto)
    {
        _actions.Die(entity);

        PlayerEntity player = PlayerEntity.Instance;
        bool targetMatch = player != null && dto != null && player.TargetId == dto.ObjectId;
        if (targetMatch)
            player.IsAttack = false;

        bool localInCombat = LocalPlayerInCombatWith(player, targetMatch);
        WaitReturnLog.Dump(
            "MonsterWorldApply.OnDie targetMatch=" + targetMatch +
            " localInCombat=" + localInCombat +
            " skipWaitReturn=" + !localInCombat,
            entity,
            dto);
        if (localInCombat && PlayerStateMachine.Instance != null)
            PlayerStateMachine.Instance.OnWaitReturn();
    }

    static bool LocalPlayerInCombatWith(PlayerEntity player, bool targetMatch)
    {
        if (player == null || !targetMatch)
            return false;
        if (player.isAutoAttack || player.IsAttack)
            return true;
        PlayerStateMachine sm = PlayerStateMachine.Instance;
        if (sm == null)
            return false;
        PlayerState state = sm.State;
        return state == PlayerState.ATTACKING ||
            state == PlayerState.PHYSICAL_SKILLS ||
            state == PlayerState.MAGIC_SKILLS ||
            state == PlayerState.ANIMATION_LOCKED;
    }

    public override void OnAttack(Entity attacker, Entity target, AttackDto dto)
    {
        if (attacker == null || attacker.IsDead())
            return;
        if (target == null || target.IsDead())
        {
            EntityActionCombatLog.LogIfWatch(attacker, target,
                "Monster.OnAttack skip dead target attacker=" + EntityActionCombatLog.NameOf(attacker) +
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

    public override void OnMagicSkillCanceled(Entity entity, MagicSkillCanceledDto dto)
    {
        if (entity != null && entity.IsDead())
            return;
        if (entity != null && entity.Identity != null)
            CombatFacingService.Instance?.EndFollow(entity.Identity.Id, "skill-canceled");
        _actions.Set(entity, EntityActionKind.Idle, null);
    }
}
