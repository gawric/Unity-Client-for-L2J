using System;
using UnityEngine;

public class NewPhysicalSkillsIntention : IntentionBase
{
    private const string SS_SM_LOG = "[SS_CHARGE_SM]";

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
                // Backup path: charge must never leave current state (ATTACK/RUN/WALK/IDLE).
                PlayerState prevState = _stateMachine.State;
                Intention prevIntention = _stateMachine.Intention;
                ApplySoulshotCharge(useSkill);
                Debug.Log(
                    $"{SS_SM_LOG} Intention backup charge skillId={useSkill.SkillId} " +
                    $"keepState={prevState} intentionNow={_stateMachine.Intention} " +
                    $"(prefer GsInterludeCombatHandler.ApplyWeaponChargeShot)");
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

    private void ApplySoulshotCharge(MagicSkillUse useSkill)
    {
        if (_stateMachine.Player == null) return;

        _stateMachine.Player.IsSoulshotCharged = true;
        Transform weapon = _stateMachine.Player.GetWeaponTransform();
        EffectManager.Instance.PlayEffect(useSkill.SkillId, weapon);

        Debug.Log(
            $"{SS_SM_LOG} ApplySoulshotCharge skillId={useSkill.SkillId} " +
            $"state={_stateMachine.State} IsSoulshotCharged=True");
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
