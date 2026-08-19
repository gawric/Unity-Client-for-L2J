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
        if (player != null && dto != null && player.TargetId == dto.ObjectId)
            player.IsAttack = false;

        if (PlayerStateMachine.Instance != null)
            PlayerStateMachine.Instance.OnWaitReturn();
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
        _actions.Set(entity, EntityActionKind.Idle, null);
    }
}
