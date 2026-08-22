using UnityEngine;

public sealed class NpcWorldApply : EntityWorldApply
{
    private readonly HtmlWindow _html;
    private readonly EntityActionMachine _actions;

    public NpcWorldApply(HtmlWindow html, EntityActionMachine actions)
    {
        _html = html;
        _actions = actions;
    }

    public override void OnMoveTo(Entity entity, Vector3 destination, Vector3 current, CharMoveToLocationDto dto)
    {
        _actions.Set(entity, EntityActionKind.Move, dto);
    }

    public override void OnStopMove(Entity entity, StopMoveDto dto)
    {
        _actions.ApplyStop(entity, dto);
    }

    public override void OnMoveToPawn(Entity entity, MoveToPawnDto dto)
    {
        _actions.Set(entity, EntityActionKind.Move, dto);
    }

    public override void OnDie(Entity entity, DieDto dto)
    {
        _actions.Die(entity);
    }

    public override void OnAttack(Entity attacker, Entity target, AttackDto dto)
    {
        if (attacker == null || attacker.IsDead())
            return;
        if (target != null && target.IsDead())
            return;
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

    public override void OnNpcHtml(Entity entity, NpcHtmlMessageDto dto)
    {
        PlayerEntity player = PlayerEntity.Instance;
        if (player != null && entity != null)
        {
            Vector3 direction = player.transform.position - entity.transform.position;
            entity.transform.rotation = Quaternion.LookRotation(direction);
        }

        if (_html == null || dto == null)
            return;

        _html.InjectToWindow(dto.Html);
        _html.ShowWindowToCenter();
    }
}
