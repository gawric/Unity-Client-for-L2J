using UnityEngine;

public class NewMagicAttackIntention : IntentionBase
{
    public NewMagicAttackIntention(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter(object arg0)
    {
        if (arg0.GetType() == typeof(MagicSkillUse))
        {
            MagicSkillUse useSkill = (MagicSkillUse)arg0;
            Debug.Log("NewMagicAttackIntention > use " + useSkill.SkillId);
            int objectId = _stateMachine.Player.IdentityInterlude.Id;
            AnimationManager.Instance.SetSpTimeAtk(objectId, useSkill.HitTime);
            Entity targetEntity = World.Instance.GetEntityNoLockSync(useSkill.TargetId);
            // Live follow until shoot; self-cast skips facing.
            if (objectId != useSkill.TargetId && targetEntity != null)
            {
                CombatFacingService.Ensure().BeginFollow(
                    objectId,
                    PlayerController.Instance.transform,
                    targetEntity.transform);
            }
            else
            {
                CombatFacingService.Instance?.EndFollow(objectId, "self-or-no-target");
            }

            IfUseSelf(objectId, useSkill);

        }
    }

    private void IfUseSelf(int objectId, MagicSkillUse useSkill)
    {
        if (objectId != useSkill.TargetId)
        {

            _stateMachine.ChangeState(PlayerState.MAGIC_SKILLS);
            _stateMachine.NotifyEvent(Event.READY_TO_ACT, useSkill);
        }
        else
        {
            TargetManager.Instance.SetTarget(new ObjectData(_stateMachine.Player.gameObject), "#ffffff");
            _stateMachine.ChangeState(PlayerState.MAGIC_SKILLS);
            _stateMachine.NotifyEvent(Event.APPLY_SELF_SKILL, useSkill);
        }
    }

    public override void Exit() { }
    public override void Update()
    {

    }
}