using UnityEngine;

public abstract class EntityWorldApply
{
    public virtual void OnMoveTo(Entity entity, Vector3 destination, Vector3 current, CharMoveToLocationDto dto)
    {
    }

    public virtual void OnStopMove(Entity entity, StopMoveDto dto)
    {
    }

    public virtual void OnMoveToPawn(Entity entity, MoveToPawnDto dto)
    {
    }

    public virtual void OnDie(Entity entity, DieDto dto)
    {
    }

    public virtual void OnRevive(Entity entity, ReviveDto dto)
    {
    }

    public virtual void OnAttack(Entity attacker, Entity target, AttackDto dto)
    {
    }

    public virtual void OnMagicSkillUse(Entity entity, MagicSkillUseDto dto)
    {
    }

    public virtual void OnAutoAttackStart(Entity entity, AutoAttackStartDto dto)
    {
    }

    public virtual void OnAutoAttackStop(Entity entity, AutoAttackStopDto dto)
    {
    }

    public virtual void OnSocialAction(Entity entity, SocialActionDto dto)
    {
    }

    public virtual void OnChangeWaitType(Entity entity, ChangeWaitTypeDto dto)
    {
    }

    public virtual void OnMagicSkillCanceled(Entity entity, MagicSkillCanceledDto dto)
    {
    }

    public virtual void OnNpcHtml(Entity entity, NpcHtmlMessageDto dto)
    {
    }
}
