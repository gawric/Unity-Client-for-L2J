using System;
using UnityEngine;

public class NewPhysicalSkillsIntention : IntentionBase
{
    public NewPhysicalSkillsIntention(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter(object arg0)
    {
        if (arg0.GetType() == typeof(MagicSkillUse))
        {
            MagicSkillUse useSkill = (MagicSkillUse)arg0;

            if (IsSpecialSkill(useSkill.SkillId))
            {
                _stateMachine.ChangeState(PlayerState.PHYSICAL_SKILLS);
                _stateMachine.NotifyEvent(Event.APPLY_SOULSHOT_CHARGED, useSkill);
                return; 
            }


            Debug.Log("NewPhysicalSkillsIntention > use " + useSkill.SkillId);
            int objectId = _stateMachine.Player.IdentityInterlude.Id;
            AnimationManager.Instance.SetSpTimeAtk(objectId , useSkill.HitTime);
            Entity targetEntity = World.Instance.GetEntityNoLockSync(useSkill.TargetId);
            PlayerController.Instance.RotateToAttacker(targetEntity.transform.position);
            IfUseSelf(objectId, useSkill);

        }
    }

    private void IfUseSelf(int objectId , MagicSkillUse useSkill)
    {
        if (objectId != useSkill.TargetId)
        {
            _stateMachine.ChangeState(PlayerState.PHYSICAL_SKILLS);
            _stateMachine.NotifyEvent(Event.READY_TO_ACT, useSkill);
        }
        else
        {
            _stateMachine.ChangeState(PlayerState.PHYSICAL_SKILLS);
            _stateMachine.NotifyEvent(Event.APPLY_SELF_SKILL, useSkill);
        }
    }

 
    private bool IsSpecialSkill(int skillId)
    {
        return skillId == (int)SpecialSkillType.SoulshotNg ||
               skillId == (int)SpecialSkillType.SpiritshotNg;
    }
    public override void Exit() { }
    public override void Update()
    {

    }
}