using UnityEngine;

public class NewMagicSkillsState  : AbstractAttackEvents
{
    public NewMagicSkillsState(PlayerStateMachine stateMachine) :
         base(stateMachine.GetObjectId(),
         SpecialAnimationNames.GetMagicSkillsAnimations(),
         stateMachine)
    { }

    public override void Enter()
    {
        base.Enter();
    }
    public override void HandleEvent(Event evt, object payload = null)
    {
        MagicSkillUseDto useSkill = GetPayload(payload);
        PlayerEntity entity = _stateMachine.Player;
        int objectId = entity.Identity.Id;

        switch (evt)
        {
            case Event.READY_TO_ACT:

                AnimationCombo readyCombo = SkillgrpTable.Instance.GetAnimComboBySkillId(useSkill.SkillId, useSkill.SkillLvl);
                string[] orderedReadyCycle = SetupDurationHelper.BuildOrderedCycleForOverrideTiming(readyCombo.GetAnimCycle());
                float[] durations = IncomingPacketActions.Animations.GetOverrideClipsDurations(objectId, orderedReadyCycle);
                float shotEventTime = SetupDurationHelper.ResolveShotEventTime(objectId, orderedReadyCycle);
                float flightTimeMs = SetupDurationHelper.ResolveMagicFlightTimeMs(
                    entity, useSkill.SkillId, entity != null ? entity.Target : null);
                entity.SetupTotalCastDuration(useSkill.HitTime, flightTimeMs, durations, shotEventTime, useSkill.TargetId);

                SkillExecutor.Instance.ExecuteSkillOverride(useSkill.SkillGrp, entity, readyCombo, _events);
                break;
            case Event.CANCEL:
                Debug.Log("NewMagicSkillsState Use Sate> Отмена скорее всего запрос пришел из ActionFaild");
                break;
            case Event.APPLY_SELF_SKILL:

                if (SetupDurationHelper.IsUsePotion(useSkill))
                {
                    SetupDurationHelper.FinishPotionUse(_stateMachine, entity, useSkill);
                    break;
                }

                AnimationCombo selfCombo = SkillgrpTable.Instance.GetAnimComboBySkillId(useSkill.SkillId, useSkill.SkillLvl);
                SetupDurationHelper.SetupDurationIfHitTimeNot0(useSkill, objectId, entity, selfCombo);
                SkillExecutor.Instance.ExecuteSkillOverride(useSkill.SkillGrp, entity, selfCombo, _events);
                break;

        }
    }
}
