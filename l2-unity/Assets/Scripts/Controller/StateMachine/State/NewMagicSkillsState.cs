using System;
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
                float[] durations = AnimationManager.Instance.GetOverrideClipsDurations(objectId, readyCombo.GetAnimCycle());
                float shotEventTime = AnimationManager.Instance.GetOverrideEventTimeByName(objectId, readyCombo.GetAnimCycle(), "OnAnimationShoot");
                float flightTimeMs = ResolveMagicFlightTimeMs(entity);
                entity.SetupTotalCastDuration(useSkill.HitTime, flightTimeMs, durations, shotEventTime);

                SkillExecutor.Instance.ExecuteSkillOverride(useSkill.SkillGrp, entity, readyCombo, _events);
                break;
            case Event.CANCEL:
                Debug.Log("NewMagicSkillsState Use Sate> Отмена скорее всего запрос пришел из ActionFaild");
                break;
            case Event.APPLY_SELF_SKILL:
                AnimationCombo selfCombo = SkillgrpTable.Instance.GetAnimComboBySkillId(useSkill.SkillId, useSkill.SkillLvl);
                SkillExecutor.Instance.ExecuteSkillOverride(useSkill.SkillGrp, entity, selfCombo, _events);
                break;

        }
    }

    private static float ResolveMagicFlightTimeMs(PlayerEntity entity)
    {
        const float fallbackFlightMs = 1000f;
        const float projectileHitOffsetSeconds = 0.3f;
        const float minFlightMs = 350f;

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
