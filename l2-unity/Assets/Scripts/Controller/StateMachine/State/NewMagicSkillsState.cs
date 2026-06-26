using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

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
        MagicSkillUse useSkill = GetPayload(payload);
        PlayerEntity entity = _stateMachine.Player;
        int objectId = entity.IdentityInterlude.Id;

        switch (evt)
        {
            case Event.READY_TO_ACT:

                AnimationCombo readyCombo = SkillgrpTable.Instance.GetAnimComboBySkillId(useSkill.SkillId, useSkill.SkillLvl);
                string[] orderedReadyCycle = SetupDurationHelper.BuildOrderedCycleForOverrideTiming(readyCombo.GetAnimCycle());
                float[] durations = AnimationManager.Instance.GetOverrideClipsDurations(objectId, orderedReadyCycle);
                float shotEventTime = SetupDurationHelper.ResolveShotEventTime(objectId, orderedReadyCycle);
                float flightTimeMs = ResolveMagicFlightTimeMs(entity, useSkill.SkillId);
                entity.SetupTotalCastDuration(useSkill.HitTime, flightTimeMs, durations, shotEventTime, useSkill.TargetId);

                SkillExecutor.Instance.ExecuteSkillOverride(useSkill.SkillGrp, entity, readyCombo, _events);
                break;
            case Event.CANCEL:
                Debug.Log("NewMagicSkillsState Use Sate> Отмена скорее всего запрос пришел из ActionFaild");
                break;
            case Event.APPLY_SELF_SKILL:

                AnimationCombo selfCombo = SkillgrpTable.Instance.GetAnimComboBySkillId(useSkill.SkillId, useSkill.SkillLvl);
                SetupDurationHelper.SetupDurationIfHitTimeNot0(useSkill, objectId, entity, selfCombo);
                SkillExecutor.Instance.ExecuteSkillOverride(useSkill.SkillGrp, entity, selfCombo, _events);
                break;

        }
    }







    private static float ResolveMagicFlightTimeMs(PlayerEntity entity, int skillId)
    {
        const float fallbackFlightMs = 1000f;
        const float projectileHitOffsetSeconds = 0.3f;
        const float minFlightMs = 350f;

        if (EffectManager.Instance != null && EffectManager.Instance.database != null &&
            EffectManager.Instance.database.ShouldIgnoreFlightTimeForCast(skillId))
        {
            return 0f;
        }

        if (entity == null || entity.Target == null)
        {
            return fallbackFlightMs;
        }

        float distance = entity.TargetDistance();
        if (distance <= 0f)
        {
            return fallbackFlightMs;
        }

        float speed = ProjectileFlightTimeCalculator.GetSpeed(distance);
        float flightSeconds = ProjectileFlightTimeCalculator.CalculateFlightTime(distance, speed, projectileHitOffsetSeconds);
        float resolvedFlightMs = Mathf.Max(minFlightMs, flightSeconds * 1000f);

        return resolvedFlightMs;
    }
}
