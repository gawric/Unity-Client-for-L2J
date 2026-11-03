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
                Debug.Log(
                    "[RES_CAST] MagicState READY_TO_ACT skill=" + useSkill.SkillId +
                    " long=" + SetupDurationHelper.IsLongCastSkill(useSkill) +
                    " clips=" + (readyCombo != null ? string.Join(" | ", readyCombo.GetAnimCycle()) : "combo-null"));
                if (SetupDurationHelper.IsLongCastSkill(useSkill) && readyCombo != null)
                {
                    SetupDurationHelper.SetupLongCastDurationIfHitTimeNot0(useSkill, objectId, entity, readyCombo);
                    SkillExecutor.Instance.ExecuteSkillOverride(useSkill.SkillGrp, entity, readyCombo, _events, isLong: true);
                    break;
                }
                string[] orderedReadyCycle = SetupDurationHelper.BuildOrderedCycleForOverrideTiming(readyCombo.GetAnimCycle());
                float[] durations = IncomingPacketActions.Animations.GetOverrideClipsDurations(objectId, orderedReadyCycle);
                float shotEventTime = SetupDurationHelper.ResolveShotEventTime(objectId, orderedReadyCycle);
                float flightTimeMs = SetupDurationHelper.ResolveMagicFlightTimeMs(
                    entity, useSkill.SkillId, entity != null ? entity.Target : null);
                entity.SetupTotalCastDuration(useSkill.HitTime, flightTimeMs, durations, shotEventTime, useSkill.TargetId, SetupDurationHelper.CastStyleOf(useSkill), SetupDurationHelper.MixComboOf(useSkill));

                SkillExecutor.Instance.ExecuteSkillOverride(useSkill.SkillGrp, entity, readyCombo, _events);
                break;
            case Event.CANCEL:
                // Debug.Log("NewMagicSkillsState Use Sate> Отмена скорее всего запрос пришел из ActionFaild");
                break;
            case Event.APPLY_SELF_SKILL:

                if (SetupDurationHelper.IsUsePotion(useSkill))
                {
                    SetupDurationHelper.FinishPotionUse(_stateMachine, entity, useSkill);
                    break;
                }

                AnimationCombo selfCombo = SkillgrpTable.Instance.GetAnimComboBySkillId(useSkill.SkillId, useSkill.SkillLvl);
                if (SetupDurationHelper.IsLongCastSkill(useSkill) && selfCombo != null)
                {
                    SetupDurationHelper.SetupLongCastDurationIfHitTimeNot0(useSkill, objectId, entity, selfCombo);
                    SkillExecutor.Instance.ExecuteSkillOverride(useSkill.SkillGrp, entity, selfCombo, _events, isLong: true);
                    break;
                }
                SetupDurationHelper.SetupDurationIfHitTimeNot0(useSkill, objectId, entity, selfCombo);
                SkillExecutor.Instance.ExecuteSkillOverride(useSkill.SkillGrp, entity, selfCombo, _events);
                break;

        }
    }
}
